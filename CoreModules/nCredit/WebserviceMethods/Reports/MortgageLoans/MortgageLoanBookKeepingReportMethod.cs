using nCredit.Code;
using nCredit.DbModel.BusinessEvents;
using nCredit.Excel;
using NTech.Banking.BookKeeping;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Linq;
using System.Xml.Linq;

namespace nCredit.WebserviceMethods.Reports
{
    public class MortgageLoanBookKeepingReportMethod : FileStreamWebserviceMethod<MortgageLoanBookKeepingReportMethod.Request>
    {
        public override string Path => "Reports/MortgageLoanBookKeeping";

        public override bool IsEnabled => NEnv.IsMortgageLoansEnabled;

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, x =>
            {
                x.Require(y => y.FromDate);
                x.Require(y => y.ToDate);
            });

            var ruleSet = NtechBookKeepingRuleFile.Parse(XDocuments.Load(NEnv.BookKeepingRuleFileName));

            using (var context = new CreditContext())
            {
                var transactions = context.Transactions.AsQueryable();

                if (request.UseTransactionDate.GetValueOrDefault())
                    transactions = transactions.Where(x =>
                        x.TransactionDate >= request.FromDate.Value && x.TransactionDate <= request.ToDate.Value);
                else
                    transactions = transactions.Where(x =>
                        x.BookKeepingDate >= request.FromDate.Value && x.BookKeepingDate <= request.ToDate.Value);

                var trs = BookKeepingFileManager.CreateEligableTransactions(transactions, ruleSet);
                var sieFile = BookKeepingFileManager.CreateSieFileFromTransactions(trs, ruleSet, new CoreClock(), null, requestContext.Service().KeyValueStore,
                    NEnv.BookKeepingAccountPlan);

                var allTransactions = sieFile.Verifications.SelectMany(x => x.Transactions.Select(y => new
                {
                    VerDate = x.Date,
                    VerText = x.Text,
                    VerRegDate = x.RegistrationDate,
                    Account = y.Account,
                    Amount = y.Amount
                })).ToList();

                var filterTag =
                    $"{(request.UseTransactionDate.GetValueOrDefault() ? "T" : "B")} {request.FromDate.Value.ToString("yyyy-MM-dd")}-{request.ToDate.Value.ToString("yyyy-MM-dd")}";

                var excelRequest = new DocumentClientExcelRequest
                {
                    Sheets = new[]
                    {
                        new DocumentClientExcelRequest.Sheet
                        {
                            AutoSizeColumns = true,
                            Title = filterTag
                        }
                    }
                };

                excelRequest.Sheets[0].SetColumnsAndData(allTransactions,
                    allTransactions.Col(x => x.VerDate, ExcelType.Date, "VerDate"),
                    allTransactions.Col(x => x.VerText, ExcelType.Text, "VerText"),
                    allTransactions.Col(x => x.VerRegDate, ExcelType.Date, "VerRegDate"),
                    allTransactions.Col(x => x.Account, ExcelType.Text, "Account"),
                    allTransactions.Col(x => x.Amount, ExcelType.Number, "Amount"),
                    allTransactions.Col(x => x.VerRegDate.HasValue ? int.Parse(x.VerRegDate.Value.ToString("yyyyMM")) : new int?(), ExcelType.Number, "Period", nrOfDecimals: 0, isNumericId: true));

                var client = requestContext.Service().DocumentClientHttpContext;
                var result = client.CreateXlsx(excelRequest);

                return ExcelFile(result, downloadFileName: $"BookKeepingTransactions-Credit-{filterTag}.xlsx");
            }
        }

        public class Request
        {
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }
            public bool? UseTransactionDate { get; set; }
        }
    }
}