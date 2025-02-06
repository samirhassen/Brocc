using nCredit.Code;
using nCredit.Excel;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.WebserviceMethods.Reports
{
    public class UnplacedBalanceReportMethod : FileStreamWebserviceMethod<UnplacedBalanceReportMethod.Request>
    {
        public override string Path => "Reports/GetUnplacedBalance";

        public override bool IsEnabled => true;

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, x =>
            {
                x.Require(r => r.Date);
            });

            var culture = NTechFormatting.GetScreenFormattingCulture(NEnv.ClientCfg.Country.BaseFormattingCulture);

            using (var context = new CreditContext())
            {
                var d = request.Date.Value.Date;

                var excelRequest = new DocumentClientExcelRequest
                {
                    Sheets = new DocumentClientExcelRequest.Sheet[]
                    {
                            new DocumentClientExcelRequest.Sheet
                            {
                                AutoSizeColumns = true,
                                Title = $"{(request.UseTransactionDate.GetValueOrDefault() ? "Tr - " : "")}{request.Date.Value.ToString("yyyy-MM-dd")}"
                            }
                    }
                };

                var s1 = excelRequest.Sheets[0];
                var startingUnplacedBalanceItems = GetStartingUnplacedBalanceItems(context, request.Date.Value, request.UseTransactionDate.GetValueOrDefault());
                s1.SetColumnsAndData(startingUnplacedBalanceItems,
                    startingUnplacedBalanceItems.Col(x => x.InitialDate, ExcelType.Date, "Initial date"),
                    startingUnplacedBalanceItems.Col(x => x.StartingBalance, ExcelType.Number, "Starting balance", includeSum: true),
                    startingUnplacedBalanceItems.Col(x => x.EndingBalance, ExcelType.Number, "Ending balance", includeSum: true),
                    startingUnplacedBalanceItems.Col(x => string.Join(", ", x.RelatedCreditNrs), ExcelType.Text, "Related credit"),
                    startingUnplacedBalanceItems.Col(x => x.IsManualPayment ? "X" : null, ExcelType.Text, "Manual pmt", nrOfDecimals: 0),
                    startingUnplacedBalanceItems.Col(x => x.NoteText, ExcelType.Text, "Note"),
                    startingUnplacedBalanceItems.Col(x => x.OcrReference, ExcelType.Text, "Ocr"),
                    startingUnplacedBalanceItems.Col(x => x.IncomingPaymentId, ExcelType.Number, "Payment id", isNumericId: true),
                    startingUnplacedBalanceItems.Col(x => x.NextKnownLaterActionDate, ExcelType.Date, "Next change date"));

                var client = requestContext.Service().DocumentClientHttpContext;
                var result = client.CreateXlsx(excelRequest);

                return File(result, downloadFileName: $"Credit-UnplacedBalance{(request.UseTransactionDate.GetValueOrDefault() ? "Tr" : "")}-{request.Date.Value.ToString("yyyy-MM-dd")}.xlsx");
            }
        }

        public class Request
        {
            public DateTime? Date { get; set; }
            public bool? UseTransactionDate { get; set; }
        }

        private class UnplacedBalanceItem
        {
            public int IncomingPaymentId { get; set; }
            public DateTime InitialDate { get; set; }
            public decimal StartingBalance { get; set; }
            public decimal EndingBalance { get; set; }
            public List<string> RelatedCreditNrs { get; set; }
            public string OcrReference { get; set; }
            public bool IsManualPayment { get; set; }
            public string NoteText { get; set; }
            public DateTime? NextKnownLaterActionDate { get; set; }
        }

        private List<UnplacedBalanceItem> GetStartingUnplacedBalanceItems(CreditContext context, DateTime date, bool useTransactionDate)
        {
            var a = context
                .IncomingPaymentHeaders.AsQueryable();

            if (useTransactionDate)
                a = a.Where(x => x.TransactionDate <= date);
            else
                a = a.Where(x => x.BookKeepingDate <= date);

            var b = a.Select(x => new
            {
                x.Id,
                x.TransactionDate,
                x.BookKeepingDate,
                TrStartingBalance = x
                        .Transactions
                        .Where(y => y.AccountCode == TransactionAccountType.UnplacedPayment.ToString() && y.TransactionDate < date)
                        .Sum(y => (decimal?)y.Amount) ?? 0m,
                TrEndingBalance = x
                        .Transactions
                        .Where(y => y.AccountCode == TransactionAccountType.UnplacedPayment.ToString() && y.TransactionDate <= date)
                        .Sum(y => (decimal?)y.Amount) ?? 0m,
                BkStartingBalance = x
                        .Transactions
                        .Where(y => y.AccountCode == TransactionAccountType.UnplacedPayment.ToString() && y.BookKeepingDate < date)
                        .Sum(y => (decimal?)y.Amount) ?? 0m,
                BkEndingBalance = x
                        .Transactions
                        .Where(y => y.AccountCode == TransactionAccountType.UnplacedPayment.ToString() && y.BookKeepingDate <= date)
                        .Sum(y => (decimal?)y.Amount) ?? 0m,
                RelatedCreditNrs = x.Transactions.Where(y => y.IncomingPaymentId.HasValue && y.CreditNr != null).Select(y => y.CreditNr).Distinct(),
                OcrReference = x.Items.Where(y => y.Name == IncomingPaymentHeaderItemCode.OcrReference.ToString() && !y.IsEncrypted).Select(y => y.Value).FirstOrDefault(),
                IsManualPayment = x.Items.Where(y => y.Name == IncomingPaymentHeaderItemCode.IsManualPayment.ToString() && !y.IsEncrypted).Select(y => y.Value).FirstOrDefault(),
                NoteText = x.Items.Where(y => y.Name == IncomingPaymentHeaderItemCode.NoteText.ToString() && !y.IsEncrypted).Select(y => y.Value).FirstOrDefault(),
                NextActionTrDate = x.Transactions.Where(y => y.AccountCode == TransactionAccountType.UnplacedPayment.ToString()).Where(y => y.TransactionDate > date).Min(y => (DateTime?)y.TransactionDate),
                NextActionBkDate = x.Transactions.Where(y => y.AccountCode == TransactionAccountType.UnplacedPayment.ToString()).Where(y => y.BookKeepingDate > date).Min(y => (DateTime?)y.BookKeepingDate)
            });

            if (useTransactionDate)
                b = b.Where(x => x.TrStartingBalance > 0m || x.TrEndingBalance > 0m);
            else
                b = b.Where(x => x.BkStartingBalance > 0m || x.BkEndingBalance > 0m);

            return b
                .Select(x => new
                {
                    x.Id,
                    StartingBalance = useTransactionDate ? x.TrStartingBalance : x.BkStartingBalance,
                    EndingBalance = useTransactionDate ? x.TrEndingBalance : x.BkEndingBalance,
                    InitialDate = useTransactionDate ? x.TransactionDate : x.BookKeepingDate,
                    NextActionDate = useTransactionDate ? x.NextActionTrDate : x.NextActionBkDate,
                    x.NoteText,
                    x.OcrReference,
                    x.IsManualPayment,
                    x.RelatedCreditNrs
                })
                .OrderBy(x => x.Id)
                .ToList()
                .Select(x => new UnplacedBalanceItem
                {
                    IncomingPaymentId = x.Id,
                    StartingBalance = x.StartingBalance,
                    EndingBalance = x.EndingBalance,
                    InitialDate = x.InitialDate,
                    RelatedCreditNrs = x.RelatedCreditNrs.ToList(),
                    IsManualPayment = x.IsManualPayment == "true",
                    NoteText = x.NoteText,
                    OcrReference = x.OcrReference,
                    NextKnownLaterActionDate = x.NextActionDate
                })
                .ToList();
        }
    }
}