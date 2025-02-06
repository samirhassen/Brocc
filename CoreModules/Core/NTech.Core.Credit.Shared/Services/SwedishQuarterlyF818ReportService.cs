using nCredit.Excel;
using NTech;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace nCredit.Code.Services
{
    public class SwedishQuarterlyF818ReportService
    {
        private readonly IDocumentClient documentClient;
        private readonly CreditContextFactory creditContextFactory;

        public SwedishQuarterlyF818ReportService(IDocumentClient documentClient, CreditContextFactory creditContextFactory)
        {
            this.documentClient = documentClient;
            this.creditContextFactory = creditContextFactory;
        }

        public ReportData GetReportData(Quarter quarter, HashSet<string> onlyTheseCreditNrs = null)
        {
            Dictionary<string, List<int>> customerIdsByCreditNr = null;

            var credits = GetCredits(quarter,
                setCustomerIdsByCreditNr: x => customerIdsByCreditNr = x,
                onlyTheseCreditNrs: onlyTheseCreditNrs);

            var lastQuarterCreditsByCreditNr = GetCredits(quarter.GetPrevious(), onlyTheseCreditNrs: onlyTheseCreditNrs)
                .ToDictionary(x => x.CreditNr);
            bool WasImpairedDuringLastQuarter(string creditNr) => lastQuarterCreditsByCreditNr.ContainsKey(creditNr) && lastQuarterCreditsByCreditNr[creditNr].IsImpaired;

            var newInQuarterCredits = credits.Where(x => x.IsNewInQuarter).ToList();

            var activeStatus = CreditStatus.Normal.ToString();
            var activeCredits = credits.Where(x => x.Status == activeStatus).ToList();
            var activeCreditCustomers = activeCredits
                .SelectMany(x => customerIdsByCreditNr[x.CreditNr].Select(y => new ActiveCreditCustomerItem { CreditNr = x.CreditNr, CustomerId = y }))
                .ToList();

            var summary = new ReportSummary
            {
                CurrentCapitalDebt_R1 = activeCredits.Sum(x => x.CapitalBalance),
                NrOfActiveCredits_R4 = activeCredits.Count(),
                NrOfNewCreditsDuringQuarter_R5 = newInQuarterCredits.Count,
                NewPaidOutCreditAmountDuringQuarter_R2 = newInQuarterCredits.Sum(x => x.InitialPaidToCustomerAmount),
                NrOfCustomersOnActiveCredits_R6 = activeCreditCustomers.Select(x => x.CustomerId).Distinct().Count(),
                InterestRevenueCurrentQuarter_R7 = credits.Sum(x => x.PaidInterestAmountDuringQuarter),
                FeesRevenueDuringQuarter_R8 = credits.Sum(x => x.PaidFeesAmountDuringQuarter),
                NrOfImpairedCredits_R9 = credits.Where(x => x.IsImpaired).Count(),
                NrOfNewImpairedCreditsDuringQuarter_R10 = credits.Where(x => x.IsImpaired && !WasImpairedDuringLastQuarter(x.CreditNr)).Count()
            };

            var resultCredits = credits.Select(x => new CreditItem
            {
                Credit = x,
                IsActive = x.Status == activeStatus,
                NrOfCustomers = customerIdsByCreditNr[x.CreditNr].Count,
                WasImpairedDuringLastQuarter = WasImpairedDuringLastQuarter(x.CreditNr)
            }).ToList();

            return new ReportData { Date = quarter.ToDate, Credits = resultCredits, Summary = summary, ActiveCreditCustomers = activeCreditCustomers };
        }

        public (MemoryStream ReportData, string DownloadFilename) CreateReport(Quarter quarter) =>
            CreateReport(GetReportData(quarter));

        public (MemoryStream ReportData, string DownloadFilename) CreateReport(ReportData data)
        {
            var summaries = Enumerables.Singleton(data.Summary).ToList();
            var credits = data.Credits;
            var activeCreditCustomers = data.ActiveCreditCustomers;

            var sheets = new List<DocumentClientExcelRequest.Sheet>();

            sheets.Add(new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = $"Quarterly KI (F818) {data.Date.ToString("yyyy-MM-dd")}"
            });

            sheets.Add(new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = $"All credits"
            });

            sheets.Add(new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = $"Active customers"
            });

            var excelRequest = new DocumentClientExcelRequest
            {
                Sheets = sheets.ToArray()
            };

            //Summary
            excelRequest.Sheets[0].SetColumnsAndData(summaries,
                summaries.Col(x => x.CurrentCapitalDebt_R1, ExcelType.Number, "Current total capital debt (r1)"),
                summaries.Col(x => x.NewPaidOutCreditAmountDuringQuarter_R2, ExcelType.Number, "Paid out amount this quarter (r2)"),
                summaries.Col(x => x.NrOfActiveCredits_R4, ExcelType.Number, "Nr of active credits (r4)", nrOfDecimals: 0),
                summaries.Col(x => x.NrOfNewCreditsDuringQuarter_R5, ExcelType.Number, "Nr of new credits this quarter (r5)", nrOfDecimals: 0),
                summaries.Col(x => x.NrOfCustomersOnActiveCredits_R6, ExcelType.Number, "Nr of active customers (r6)", nrOfDecimals: 0),
                summaries.Col(x => x.InterestRevenueCurrentQuarter_R7, ExcelType.Number, "Interest revenue this quarter (r7)"),
                summaries.Col(x => x.FeesRevenueDuringQuarter_R8, ExcelType.Number, "Fee revenue this quarter (r8)"),
                summaries.Col(x => x.NrOfImpairedCredits_R9, ExcelType.Number, "Nr of impared credits (r9)", nrOfDecimals: 0),
                summaries.Col(x => x.NrOfNewImpairedCreditsDuringQuarter_R10, ExcelType.Number, "Nr of impared credits this quarter (r10)", nrOfDecimals: 0));

            //Loans
            excelRequest.Sheets[1].SetColumnsAndData(credits,
                credits.Col(x => x.Credit.CreditNr, ExcelType.Text, "Credit nr"),
                credits.Col(x => x.Credit.StartDate, ExcelType.Date, "Start date"),
                credits.Col(x => x.Credit.CapitalBalance, ExcelType.Number, "Current capital debt", includeSum: true),
                credits.Col(x => x.Credit.InitialPaidToCustomerAmount, ExcelType.Number, "Initial paid out amount", includeSum: true),
                credits.Col(x => x.Credit.Status, ExcelType.Text, "Status"),
                credits.Col(x => x.IsActive ? 1 : 0, ExcelType.Number, "Is active", nrOfDecimals: 0, includeSum: true),
                credits.Col(x => x.NrOfCustomers, ExcelType.Number, "Nr of customers", nrOfDecimals: 0),
                credits.Col(x => x.Credit.PaidInterestAmountDuringQuarter, ExcelType.Number, "Interest revenue this quarter", includeSum: true),
                credits.Col(x => x.Credit.PaidFeesAmountDuringQuarter, ExcelType.Number, "Fee revenue this quarter", includeSum: true),
                credits.Col(x => x.Credit.NrOfOverdueDays, ExcelType.Number, "Days past due date", nrOfDecimals: 0),
                credits.Col(x => x.Credit.IsImpaired ? 1 : 0, ExcelType.Number, "Is impaired", nrOfDecimals: 0, includeSum: true),
                credits.Col(x => x.WasImpairedDuringLastQuarter ? 1 : 0, ExcelType.Number, "Was impaired last quarter", nrOfDecimals: 0, includeSum: true)
            );

            //Customers
            excelRequest.Sheets[2].SetColumnsAndData(activeCreditCustomers,
                activeCreditCustomers.Col(x => x.CreditNr, ExcelType.Text, "Credit nr"),
                //The formula is just count_distinct(all customer ids).
                activeCreditCustomers.Col(x => x.CustomerId, ExcelType.Number, "Customer id", isNumericId: true, includeSum: true, customSum: "SUMPRODUCT(1/COUNTIF(B2:OFFSET([[CELL]], -1, 0, 1, 1),B2:OFFSET([[CELL]], -1, 0, 1, 1)))", nrOfDecimals: 0));
            var result = documentClient.CreateXlsx(excelRequest);

            return (ReportData: result, DownloadFilename: $"Quarterly-KI-F818-{data.Date.ToString("yyyy-MM-dd")}.xlsx");
        }

        public class ReportData
        {
            public DateTime Date { get; set; }
            public List<CreditItem> Credits { get; set; }
            public ReportSummary Summary { get; set; }
            public List<ActiveCreditCustomerItem> ActiveCreditCustomers { get; set; }
        }

        public class CreditItem
        {
            public CreditModel Credit { get; set; }
            public bool IsActive { get; set; }
            public int NrOfCustomers { get; set; }
            public bool WasImpairedDuringLastQuarter { get; set; }
        }

        public class ActiveCreditCustomerItem
        {
            public string CreditNr { get; set; }
            public int CustomerId { get; set; }
        }

        public class ReportSummary
        {
            public decimal? CurrentCapitalDebt_R1 { get; set; }
            public decimal? NewPaidOutCreditAmountDuringQuarter_R2 { get; set; }
            public int NrOfActiveCredits_R4 { get; set; }
            public int NrOfNewCreditsDuringQuarter_R5 { get; set; }
            public int NrOfCustomersOnActiveCredits_R6 { get; set; }
            public decimal? InterestRevenueCurrentQuarter_R7 { get; set; }
            public decimal? FeesRevenueDuringQuarter_R8 { get; set; }
            public int NrOfImpairedCredits_R9 { get; set; }
            public int NrOfNewImpairedCreditsDuringQuarter_R10 { get; set; }
        }

        public class CreditModel
        {
            public DateTime StartDate { get; set; }
            public string CreditNr { get; set; }
            public string Status { get; set; }
            public decimal? CapitalBalance { get; set; }
            public decimal? PaidInterestAmountDuringQuarter { get; set; }
            public int NrOfOverdueDays { get; set; }
            public decimal? InitialPaidToCustomerAmount { get; set; }
            public bool IsNewInQuarter { get; set; }
            public decimal? PaidFeesAmountDuringQuarter { get; set; }
            public bool IsImpaired { get; set; }
        }

        private List<CreditModel> GetCredits(Quarter quarter,
            HashSet<string> onlyTheseCreditNrs = null,
            Action<Dictionary<string, List<int>>> setCustomerIdsByCreditNr = null)
        {
            var paidFeesAccountTypesStrings = new List<TransactionAccountType> {
                    TransactionAccountType.InitialFeeDrawnFromLoanAmount,
                    TransactionAccountType.NotificationFeeDebt,
                    TransactionAccountType.ReminderFeeDebt
                }.Select(x => x.ToString()).ToList();

            using (var context = creditContextFactory.CreateContext())
            {
                var toDate = Dates.Min(context.CoreClock.Today, quarter.ToDate); //Make sure it does not run for the future which messes with nr of overdue days
                var fromDate = quarter.FromDate;

                var creditsBasis = context
                    .CreditHeadersQueryable.Where(x => x.CreatedByEvent.TransactionDate <= toDate);

                if (onlyTheseCreditNrs != null && onlyTheseCreditNrs.Count > 0)
                {
                    creditsBasis = creditsBasis.Where(x => onlyTheseCreditNrs.Contains(x.CreditNr));
                }

                var creditsPre = creditsBasis
                    .OrderBy(x => x.CreatedByBusinessEventId)
                    .Select(x => new
                    {
                        x.CreditNr,
                        StartDate = x.CreatedByEvent.TransactionDate,
                        CapitalBalance = x
                            .Transactions
                            .Where(y => y.TransactionDate <= toDate && y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                            .Sum(y => (decimal?)y.Amount),
                        Status = x
                            .DatedCreditStrings
                            .Where(y => y.TransactionDate <= toDate && y.Name == DatedCreditStringCode.CreditStatus.ToString())
                            .OrderByDescending(y => y.TransactionDate)
                            .ThenByDescending(y => y.Timestamp)
                            .Select(y => y.Value)
                            .FirstOrDefault(),
                        InitialPaidToCustomerAmount = x
                            .CreatedByEvent
                            .Transactions
                            .Where(y => y.AccountCode == TransactionAccountType.ShouldBePaidToCustomer.ToString())
                            .Sum(y => (decimal?)y.Amount),
                        OldestOpenNotificationDueDate = x
                            .Notifications
                            .Where(y => y.DueDate < toDate && y.TransactionDate <= toDate && (y.ClosedTransactionDate == null || y.ClosedTransactionDate > toDate))
                            .Min(y => (DateTime?)y.DueDate),
                        PaidInterestAmountDuringQuarter = x.Transactions.Where(y =>
                                !y.WriteoffId.HasValue
                                && y.IncomingPaymentId.HasValue
                                && y.TransactionDate <= toDate
                                && TransactionAccountType.InterestDebt.ToString() == y.AccountCode
                                && y.TransactionDate >= fromDate
                                && y.TransactionDate <= toDate)
                            .Sum(y => -(decimal?)y.Amount),
                        PaidFeesAmountDuringQuarter = x.Transactions.Where(y =>
                                !y.WriteoffId.HasValue
                                && (y.IncomingPaymentId.HasValue || y.AccountCode == TransactionAccountType.InitialFeeDrawnFromLoanAmount.ToString())
                                && y.TransactionDate <= toDate
                                && paidFeesAccountTypesStrings.Contains(y.AccountCode)
                                && y.TransactionDate >= fromDate
                                && y.TransactionDate <= toDate)
                            .Sum(y => (y.AccountCode == TransactionAccountType.InitialFeeDrawnFromLoanAmount.ToString() ? (decimal?)y.Amount : -(decimal?)y.Amount)),
                        CustomerIds = x.CreditCustomers.Select(y => y.CustomerId)
                    })
                    .ToList();

                if (setCustomerIdsByCreditNr != null)
                {
                    var customerIdsByCreditNr = creditsPre.ToDictionary(x => x.CreditNr, x => x.CustomerIds.ToList());
                    setCustomerIdsByCreditNr(customerIdsByCreditNr);
                }

                return creditsPre.Select(x =>
                {
                    var nrOfOverdueDays = x.OldestOpenNotificationDueDate.HasValue
                                                ? (int)Math.Round(Dates.GetAbsoluteTimeBetween(toDate, x.OldestOpenNotificationDueDate.Value).TotalDays)
                                                : 0;
                    return new CreditModel
                    {
                        CreditNr = x.CreditNr,
                        StartDate = x.StartDate,
                        CapitalBalance = x.CapitalBalance,
                        PaidInterestAmountDuringQuarter = x.PaidInterestAmountDuringQuarter,
                        PaidFeesAmountDuringQuarter = x.PaidFeesAmountDuringQuarter,
                        Status = x.Status,
                        NrOfOverdueDays = nrOfOverdueDays,
                        IsImpaired = nrOfOverdueDays > 90,
                        InitialPaidToCustomerAmount = x.InitialPaidToCustomerAmount,
                        IsNewInQuarter = quarter.ContainsDate(x.StartDate)
                    };
                }).ToList();
            }
        }
    }
}