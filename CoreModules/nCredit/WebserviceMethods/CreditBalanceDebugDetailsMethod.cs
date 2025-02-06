using nCredit.Code;
using nCredit.Excel;
using Newtonsoft.Json;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCredit.WebserviceMethods
{
    public class CreditBalanceDebugDetailsMethod : FileStreamWebserviceMethod<CreditBalanceDebugDetailsMethod.Request>
    {
        public override string Path => "Credit/BalanceDebugDetails";

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            using (var context = new CreditContext())
            {
                var dates = context
                    .CreditHeaders
                    .Where(x => x.CreditNr == request.CreditNr)
                    .Select(x => new
                    {
                        x.CreditNr,
                        MarginInterestRateChanges = x.DatedCreditValues
                            .Where(y => y.Name == DatedCreditValueCode.MarginInterestRate.ToString())
                            .GroupBy(y => y.TransactionDate)
                            .Select(y => new
                            {
                                TransactionDate = y.Key,
                                Value = y.OrderByDescending(z => z.BusinessEventId).Select(z => z.Value).FirstOrDefault()
                            }),
                        ReferenceInterestRateChanges = x.DatedCreditValues
                            .Where(y => y.Name == DatedCreditValueCode.ReferenceInterestRate.ToString())
                            .GroupBy(y => y.TransactionDate)
                            .Select(y => new
                            {
                                TransactionDate = y.Key,
                                Value = y.OrderByDescending(z => z.BusinessEventId).Select(z => z.Value).FirstOrDefault()
                            }),
                        CapitalDebtChanges = x.Transactions
                            .Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                            .GroupBy(y => y.TransactionDate)
                            .Select(y => new
                            {
                                TransactionDate = y.Key,
                                ChangeAmount = y.Sum(z => z.Amount),
                                BalanceAfterAmount = x
                                    .Transactions
                                    .Where(z => z.AccountCode == TransactionAccountType.CapitalDebt.ToString() && z.TransactionDate <= y.Key)
                                    .Sum(z => z.Amount)
                            })
                    })
                    .Single();

                var allDates = dates.CapitalDebtChanges.Select(x => x.TransactionDate)
                    .Concat(dates.ReferenceInterestRateChanges.Select(y => y.TransactionDate))
                    .Concat(dates.MarginInterestRateChanges.Select(y => y.TransactionDate))
                    .ToHashSet();

                var changes = allDates.OrderBy(x => x).Select(x =>
                {
                    var capitalDebtChange = dates.CapitalDebtChanges.Where(y => y.TransactionDate <= x).OrderBy(y => y.TransactionDate).Last();
                    var referenceInterestRateChange = dates.ReferenceInterestRateChanges.Where(y => y.TransactionDate <= x).OrderBy(y => y.TransactionDate).Last();
                    var marginInterestRateChange = dates.MarginInterestRateChanges.Where(y => y.TransactionDate <= x).OrderBy(y => y.TransactionDate).Last();
                    return new
                    {
                        TransactionDate = x,
                        InterestRatePercent = referenceInterestRateChange.Value + marginInterestRateChange.Value,
                        WasInterestRatePercentChanged = referenceInterestRateChange.TransactionDate == x || marginInterestRateChange.TransactionDate == x,
                        BalanceAfterAmount = capitalDebtChange.BalanceAfterAmount,
                        BalanceChangeAmount = capitalDebtChange.TransactionDate == x ? (decimal?)capitalDebtChange.ChangeAmount : null
                    };
                })
                .ToList();

                var excelRequest = new DocumentClientExcelRequest
                {
                    Sheets = new[]
                    {
                        new DocumentClientExcelRequest.Sheet
                        {
                            AutoSizeColumns = true,
                            Title = $"{request.CreditNr} - {allDates.Last().ToString("yyyy-MM-dd")}"
                        }
                    }
                };
                var s = excelRequest.Sheets[0];

                s.SetColumnsAndData(changes,
                    changes.Col(x => x.TransactionDate, ExcelType.Date, "Date"),
                    changes.Col(x => x.BalanceAfterAmount, ExcelType.Number, "Balance amount"),
                    changes.Col(x => x.InterestRatePercent / 100, ExcelType.Percent, "Interest rate"),
                    changes.Col(x => x.BalanceChangeAmount, ExcelType.Number, "Balance change amount"));

                var client = requestContext.Service().DocumentClientHttpContext;
                var result = client.CreateXlsx(excelRequest);

                return this.ExcelFile(result, $"CreditBalanceDetails-{request.CreditNr}-{allDates.Last().ToString("yyyy-MM-dd")}.xlsx");
            }
        }

        public class Request
        {
            [Required]
            public string CreditNr { get; set; }
        }
    }
}