using Dapper;
using nCredit;
using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using nCredit.Excel;
using Newtonsoft.Json;
using NTech.Banking.BookKeeping;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Core.Credit.Shared.Services
{
    public class BookkeepingReconciliationReportService
    {
        private readonly CreditContextFactory contextFactory;
        private readonly IKeyValueStoreService keyValueStoreService;
        private readonly ICoreClock clock;
        private readonly IDocumentClient documentClient;
        private readonly ICreditEnvSettings envSettings;

        public BookkeepingReconciliationReportService(CreditContextFactory contextFactory, IKeyValueStoreService keyValueStoreService, 
            ICoreClock clock, IDocumentClient documentClient, ICreditEnvSettings envSettings)
        {
            this.contextFactory = contextFactory;
            this.keyValueStoreService = keyValueStoreService;
            this.clock = clock;
            this.documentClient = documentClient;
            this.envSettings = envSettings;
        }

        private class BookKeepingReconciliationReportRow
        {
            public string CreditNr { get; set; }
            public Dictionary<string, decimal> InPeriodAmountByAccountNr { get; set; }
            public Dictionary<string, decimal> EndOfPeriodAmountByAccountNr { get; set; }
            public decimal GetInPeriodAmount(string accountNr) => InPeriodAmountByAccountNr.OptS(accountNr) ?? 0m;
            public decimal GetPeriodEndAmount(string accountNr) => EndOfPeriodAmountByAccountNr.OptS(accountNr) ?? 0m;
        }

        public async Task<(MemoryStream ReportData, string ReportFileName)> CreateExcelReportAsync(BookKeepingReconciliationReportRequest request)
        {
            var fromDate = request?.FromDate ?? clock.Today.AddDays(-30);
            var toDate = request?.ToDate ?? fromDate.AddDays(30);
            
            var reportColumns = HasCustomReportFormat(envSettings) 
                ? GetCustomReportColumns(envSettings.BookKeepingReconciliationReportFormatFile)
                : GetDefaultReportColumns(envSettings.BookKeepingAccountPlan);

            var reportRows = GetReportRows(fromDate, toDate, reportColumns);
            
            var excelColumns = DocumentClientExcelRequest.CreateDynamicColumnList(reportRows);
            excelColumns.Add(reportRows.Col(x => x.CreditNr, ExcelType.Text, "CreditNr"));
            foreach(var reportColumn in reportColumns)
            {
                excelColumns.Add(reportRows.Col(
                    x => reportColumn.IsInPeriodAmount ? x.GetInPeriodAmount(reportColumn.AccountNr) : x.GetPeriodEndAmount(reportColumn.AccountNr),
                    ExcelType.Number, reportColumn.ColumnHeader, includeSum: true));
            }

            var excelRequest = new DocumentClientExcelRequest
            {
                Sheets = new DocumentClientExcelRequest.Sheet[]
                {
                    new DocumentClientExcelRequest.Sheet
                    {
                        AutoSizeColumns = true,
                        Title = "Reconciliation report"
                    }
                }
            };
            excelRequest.Sheets[0].SetColumnsAndData(reportRows, excelColumns.ToArray());
            var reportData = await documentClient.CreateXlsxAsync(excelRequest);
            return (ReportData: reportData, ReportFileName: $"ReconciliationReport_{fromDate:yyyy-MM-dd}_{toDate:yyyy-MM-dd}.xlsx");
        }

        public static bool IsReportEnabled(ICreditEnvSettings envSettings)
        {
            return (envSettings.BookKeepingAccountPlan != null && (envSettings.IsStandardMortgageLoansEnabled || envSettings.IsStandardUnsecuredLoansEnabled))
                || HasCustomReportFormat(envSettings);
        }

        private static bool HasCustomReportFormat(ICreditEnvSettings envSettings) => 
            envSettings.BookKeepingReconciliationReportFormatFile != null && envSettings.BookKeepingReconciliationReportFormatFile.Exists;

        private List<(string AccountNr, string ColumnHeader, bool IsInPeriodAmount)> GetDefaultReportColumns(NtechAccountPlanFile accountPlan)
        {
            var getAccountNrByName = BookKeepingFileManager.CreateBookKeepingAccountNrByNameSource(keyValueStoreService, accountPlan);
            var result = new List<(string AccountNr, string ColumnHeader, bool IsInPeriodAmount)>();
            foreach (var account in accountPlan.Accounts)
            {
                var accountNr = getAccountNrByName(account.Name);
                result.Add((AccountNr: accountNr, ColumnHeader: accountNr + " (I)", true));
                result.Add((AccountNr: accountNr, ColumnHeader: accountNr + " (E)", true));
            }
            return result;
        }

        private List<(string AccountNr, string ColumnHeader, bool IsInPeriodAmount)> GetCustomReportColumns(FileInfo customReportFormatJsonFile)
        {
            var parsedFormat = JsonConvert.DeserializeAnonymousType(File.ReadAllText(customReportFormatJsonFile.FullName, Encoding.UTF8), new
            {
                columns = new[]
                {
                    new
                    {
                        accountNr = "", //Example: 6351
                        columnHeader = "", //Example: Konstaderade förluster kundfodringar (6351)
                        type = "" //periodSum or periodEndValue
                    }
                }
            });

            return parsedFormat.columns.Select(x => (AccountNr: x.accountNr, ColumnHeader: x.columnHeader, IsInPeriodAmount: x.type == "periodSum")).ToList();
        }

        private List<BookKeepingReconciliationReportRow> GetReportRows(DateTime fromDate, DateTime toDate, List<(string AccountNr, string ColumnHeader, bool IsInPeriodAmount)> reportColumns)
        {
            var inPeriodAccountNrs = reportColumns.Where(x => x.IsInPeriodAmount).Select(x => x.AccountNr).ToHashSetShared();
            var endOfPeriodAccountNrs = reportColumns.Where(x => !x.IsInPeriodAmount).Select(x => x.AccountNr).ToHashSetShared();

            var dataRowsByCreditNr = GetDataRows(fromDate, toDate, onlyTheseAccountNrs: inPeriodAccountNrs.Union(endOfPeriodAccountNrs).ToHashSetShared())
                .GroupBy(x => x.CreditNr)
                .ToList();

            var reportRows = new List<BookKeepingReconciliationReportRow>(dataRowsByCreditNr.Count);
            foreach(var dataRows in dataRowsByCreditNr) 
            {
                var creditNr = dataRows.Key;
                var row = new BookKeepingReconciliationReportRow
                {
                    CreditNr = creditNr,
                    EndOfPeriodAmountByAccountNr = new Dictionary<string, decimal>(),
                    InPeriodAmountByAccountNr = new Dictionary<string, decimal>()
                };
                var hasBalance = false;
                foreach (var dataRow in dataRows)
                {
                    var inPeriodAmount = dataRow.InPeriodAmount;
                    var endOfPeriodAmount = dataRow.BeforePeriodAmount + dataRow.InPeriodAmount;
                    if (inPeriodAccountNrs.Contains(dataRow.AccountNr) && inPeriodAmount != 0m)
                    {
                        row.InPeriodAmountByAccountNr.AddOrUpdate(dataRow.AccountNr, inPeriodAmount, x => x + inPeriodAmount);
                        hasBalance = true;
                    }
                    if (endOfPeriodAccountNrs.Contains(dataRow.AccountNr) && endOfPeriodAmount != 0m)
                    {
                        row.EndOfPeriodAmountByAccountNr.AddOrUpdate(dataRow.AccountNr, endOfPeriodAmount, x => x + endOfPeriodAmount);
                        hasBalance = true;
                    }
                }
                if(hasBalance) reportRows.Add(row);
            }
            return reportRows;
        }

        private List<DataRow> GetDataRows(DateTime fromDate, DateTime toDate, HashSet<string> onlyTheseAccountNrs = null)
        {
            var query =
@"
with SieFileTransactionExtended
as
(
	select	t.*,
			(select top 1 c.ConnectionId from SieFileConnection c where c.VerificationId = t.VerificationId and c.ConnectionType = 'CreditNr') as CreditNr,
			v.[Date] as BookkeepingDate,
			v.RegistrationDate as TransactionDate,
			case when v.[Date] >= @fromDate and v.[Date] <= @toDate then t.Amount else 0 end as InPeriodAmount,
			case when v.[Date] < @fromDate then t.Amount else 0 end as BeforePeriodAmount
	from	SieFileTransaction t
	join	SieFileVerification v on v.Id = t.VerificationId
	where	v.[Date] <= @toDate
    [[[ACCOUNTNR_FILTER]]]
)
select	t.AccountNr,
		t.CreditNr,
		sum(t.InPeriodAmount) as InPeriodAmount,
		sum(t.BeforePeriodAmount) as BeforePeriodAmount
from	SieFileTransactionExtended t
group by t.AccountNr, t.CreditNr".Replace("[[[ACCOUNTNR_FILTER]]]", onlyTheseAccountNrs == null ? "" : "and t.AccountNr in @onlyTheseAccountNrs");

            using(var context = contextFactory.CreateContext())
            {
                var parameters = new DynamicParameters();
                parameters.Add("@fromDate", fromDate);
                parameters.Add("@toDate", toDate);
                if (onlyTheseAccountNrs != null)
                    parameters.Add("@onlyTheseAccountNrs", onlyTheseAccountNrs);

                return context
                    .GetConnection()
                    .Query<DataRow>(query, param: parameters, commandTimeout: 90)
                    .Where(x => x.InPeriodAmount != 0m || x.BeforePeriodAmount != 0m)
                    .ToList();
            }
        }

        private class DataRow
        {
            public string AccountNr { get; set; }
            public string CreditNr { get; set; }
            public decimal InPeriodAmount { get; set; }
            public decimal BeforePeriodAmount { get; set; }
        }
    }

    public class BookKeepingReconciliationReportRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
