using nCredit.Code;
using nCredit.Excel;
using NTech.Services.Infrastructure.NTechWs;
using System.Linq;

namespace nCredit.WebserviceMethods.Reports.MortgageLoans
{
    public class MortgageFixedInterestRateHistoryReportMethod : FileStreamWebserviceMethod<MortgageFixedInterestRateHistoryReportMethod.Request>
    {
        public override string Path => "Reports/MortgageLoanFixedInterestRateHistory";

        public override bool IsEnabled => IsReportEnabled;
        public static bool IsReportEnabled => NEnv.IsStandardMortgageLoansEnabled;
        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, x =>
            {

            });

            using (var context = new CreditContext())
            {
                var changeEvents = context
                    .BusinessEvents
                    .Where(x => x.EventType == BusinessEventType.ChangedMortgageLoanFixedInterestRate.ToString())
                    .OrderBy(x => x.Id)
                    .Select(x => new
                    {
                        EventId = x.Id,
                        EventDate = x.TransactionDate,
                        Rates = x.CreatedHFixedMortgageLoanInterestRates
                    })
                    .ToList();


                var excelRequest = new DocumentClientExcelRequest
                {
                    Sheets = new[]
                    {
                        new DocumentClientExcelRequest.Sheet
                        {
                            AutoSizeColumns = true,
                            Title = "Fixed interest history"
                        }
                    }
                };

                var allMonthCounts = changeEvents.SelectMany(x => x.Rates.Select(y => y.MonthCount)).ToHashSet();


                var cols = DocumentClientExcelRequest.CreateDynamicColumnList(changeEvents);

                cols.Add(changeEvents.Col(x => x.EventDate, ExcelType.Date, "Change Date"));
                foreach (var monthCount in allMonthCounts.OrderBy(x => x))
                {
                    var headerText = monthCount % 12 == 0 ? $"{monthCount / 12} år" : $"{monthCount} månader";
                    cols.Add(changeEvents.Col(x => x.Rates.FirstOrDefault(y => y.MonthCount == monthCount)?.RatePercent / 100m, ExcelType.Percent, headerText));
                }

                excelRequest.Sheets[0].SetColumnsAndData(changeEvents, cols.ToArray());

                var client = requestContext.Service().DocumentClientHttpContext;
                var result = client.CreateXlsx(excelRequest);

                return ExcelFile(result, downloadFileName: $"MortgageLoanFixedInterestRateHistory.xlsx");
            }
        }

        public class Request
        {

        }
    }
}