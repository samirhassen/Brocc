using nCredit.Code;
using nCredit.Excel;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechAuthorizeCreditHigh]
    [NTechApi]
    public class ApiReportsLcrController : NController
    {
        protected override bool IsEnabled => !NEnv.IsStandardUnsecuredLoansEnabled;

        private class LiquidityCoverageModel
        {
            public string CreditNr { get; set; }
            public decimal? InitialCapitalDebt { get; set; }
            public decimal? CapitalBalance { get; set; }
            public int? NrOfRemainingMonths { get; set; }
            public DateTime? ApproximateLastPaymentMonth { get; set; }
            public decimal? TotalInterestRate { get; set; }
        }

        [Route("Api/Reports/Lcr")]
        [HttpGet()]
        public ActionResult Get(DateTime date)
        {
            if (!NEnv.IsUnsecuredLoansEnabled)
                return HttpNotFound();

            try
            {
                var dc = new DataWarehouseClient();
                var p = new ExpandoObject();
                (p as IDictionary<string, object>)["forDate"] = date.Date;

                var items = dc.FetchReportData<LiquidityCoverageModel>("dailyLiquidityCoverageBasis", p);

                var request = new DocumentClientExcelRequest
                {
                    Sheets = new DocumentClientExcelRequest.Sheet[]
                    {
                        new DocumentClientExcelRequest.Sheet
                        {
                            AutoSizeColumns = true,
                            Title = $"LCR"
                        }
                    }
                };
                var excelTemplate = NEnv.GetOptionalExcelTemplateFilePath("LcrReport.xlsx");
                if (excelTemplate.Exists)
                {
                    request.TemplateXlsxDocumentBytesAsBase64 = Convert.ToBase64String(System.IO.File.ReadAllBytes(excelTemplate.FullName));
                }

                var dueDay = NEnv.NotificationProcessSettings.GetByCreditType(DomainModel.CreditType.UnsecuredLoan).NotificationDueDay;
                Func<DateTime?, DateTime?> monthToDueDate = d => d.HasValue ? new DateTime?(new DateTime(d.Value.Year, d.Value.Month, dueDay)) : d;
                Func<string, string> headerText = x => request.TemplateXlsxDocumentBytesAsBase64 == null ? x : null;

                var lecd = request.Sheets[0];
                lecd.SetColumnsAndData(items,
                    items.Col(x => x.CreditNr, ExcelType.Text, headerText("Credit nr")),
                    items.Col(x => x.InitialCapitalDebt, ExcelType.Number, headerText("Original Capital Debt"), includeSum: false),
                    items.Col(x => x.CapitalBalance, ExcelType.Number, headerText("Current balance"), includeSum: false),
                    items.Col(x => x.NrOfRemainingMonths, ExcelType.Number, headerText("Repayment Time Left in Months"), nrOfDecimals: 0),
                    items.Col(x => monthToDueDate(x.ApproximateLastPaymentMonth), ExcelType.Date, headerText("Last due date")),
                    items.Col(x => x.TotalInterestRate, ExcelType.Number, headerText("Interest rate")),
                    items.Col(x => "Aktivt", ExcelType.Text, headerText("Aktivt")),
                    items.Col(x => "EUR", ExcelType.Text, headerText("EUR")));

                var client = Service.DocumentClientHttpContext;
                var result = client.CreateXlsx(request);

                return new FileStreamResult(result, XlsxContentType) { FileDownloadName = $"Lcr_{date.ToString("yyyy-MM")}.xlsx" };
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Failed to create liquidity coverage ratio report");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }
    }
}