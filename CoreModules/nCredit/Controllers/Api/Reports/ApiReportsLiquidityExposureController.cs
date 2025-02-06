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
    public class ApiReportsLiquidityExposureController : NController
    {
        protected override bool IsEnabled => !NEnv.IsStandardUnsecuredLoansEnabled;

        [Route("Api/Reports/LiquidityExposure")]
        [HttpGet()]
        public ActionResult Get(DateTime monthEndDate)
        {
            if (!NEnv.IsUnsecuredLoansEnabled)
                return HttpNotFound();

            try
            {
                DateTime fromDate = new DateTime(monthEndDate.Year, monthEndDate.Month, 1);
                DateTime toDate = fromDate.AddMonths(1).AddDays(-1);

                var dc = new DataWarehouseClient();
                var p = new ExpandoObject();
                (p as IDictionary<string, object>)["fromDate"] = fromDate;

                var items = dc.FetchReportData<DbModel.Repository.LiquidityExposureReportDataWarehouseModel.LiquidityExposureReportItemModel>("monthlyLiquidityExposureBasis", p);

                var request = new DocumentClientExcelRequest
                {
                    Sheets = new DocumentClientExcelRequest.Sheet[]
                    {
                        new DocumentClientExcelRequest.Sheet
                        {
                            AutoSizeColumns = true,
                            Title = $"Capital Payments"
                        },
                        new DocumentClientExcelRequest.Sheet
                        {
                            AutoSizeColumns = true,
                            Title = $"Capital+Interest Payments"
                        },
                        new DocumentClientExcelRequest.Sheet
                        {
                            AutoSizeColumns = true,
                            Title = $"Raw ({toDate.ToString("yyyy-MM-dd")})"
                        }
                    }
                };

                var dueDay = NEnv.NotificationProcessSettings.GetByCreditType(DomainModel.CreditType.UnsecuredLoan).NotificationDueDay;
                Func<int, DateTime> approximateLastDueDate = n => new DateTime(fromDate.AddMonths(n).Year, fromDate.AddMonths(n).Month, dueDay);

                var lecd = request.Sheets[0];
                lecd.SetColumnsAndData(items,
                    items.Col(x => x.CreditNr, ExcelType.Text, "Credit nr"),
                    items.Col(x => x.CurrentCapitalDebt, ExcelType.Number, "Current Capital Debt", includeSum: true),
                    items.Col(x => x.CapitalAmount_1_3 + (x.CurrentCapitalDebt - x.CurrentNotNotifiedCapitalDebt), ExcelType.Number, "Capital Payments 1-3 months", includeSum: true),
                    items.Col(x => x.CapitalAmount_4_12, ExcelType.Number, "4-12 months", includeSum: true),
                    items.Col(x => x.CapitalAmount_13_60, ExcelType.Number, "13-60 months", includeSum: true),
                    items.Col(x => x.CapitalAmount_61_end, ExcelType.Number, ">=61 months", includeSum: true),
                    items.Col(x => x.NrOfRemainingMonths, ExcelType.Number, "Repayment Time Left in Months", nrOfDecimals: 0, customSum: "SUM(OFFSET([[CELL]],0,-4,1,4))", sumNrOfDecimals: 2),
                    items.Col(x => approximateLastDueDate(x.NrOfRemainingMonths), ExcelType.Date, "Last due date"));

                var lep = request.Sheets[1];
                lep.SetColumnsAndData(items,
                    items.Col(x => x.CreditNr, ExcelType.Text, "Credit nr"),
                    items.Col(x => x.CurrentCapitalDebt, ExcelType.Number, "Current Capital Debt", includeSum: true),
                    items.Col(x => x.CapitalAmount_1_3 + x.InterestAmount_1_3 + (x.CurrentCapitalDebt - x.CurrentNotNotifiedCapitalDebt), ExcelType.Number, "Interest + Capital Payments 1-3 months", includeSum: true),
                    items.Col(x => x.CapitalAmount_4_12 + x.InterestAmount_4_12, ExcelType.Number, "4-12 months", includeSum: true),
                    items.Col(x => x.CapitalAmount_13_60 + x.InterestAmount_13_60, ExcelType.Number, "13-60 months", includeSum: true),
                    items.Col(x => x.CapitalAmount_61_end + x.InterestAmount_61_end, ExcelType.Number, ">=61 months", includeSum: true),
                    items.Col(x => x.NrOfRemainingMonths, ExcelType.Number, "Repayment Time Left in Months", nrOfDecimals: 0, customSum: "SUM(OFFSET([[CELL]],0,-4,1,4))", sumNrOfDecimals: 2),
                    items.Col(x => approximateLastDueDate(x.NrOfRemainingMonths), ExcelType.Date, "Last due date"));

                var raw = request.Sheets[2];
                raw.SetColumnsAndData(items,
                    items.Col(x => x.CreditNr, ExcelType.Text, "Credit nr"),
                    items.Col(x => x.CurrentCapitalDebt, ExcelType.Number, "Current Capital Debt", includeSum: true),
                    items.Col(x => x.CurrentNotNotifiedCapitalDebt, ExcelType.Number, "Current Not Notified Capital Debt", includeSum: true),
                    items.Col(x => x.CapitalAmount_1_3, ExcelType.Number, "Capital 1-3", includeSum: true),
                    items.Col(x => x.InterestAmount_1_3, ExcelType.Number, "Interest 1-3", includeSum: true),
                    items.Col(x => x.CapitalAmount_4_12, ExcelType.Number, "Capital 4-12", includeSum: true),
                    items.Col(x => x.InterestAmount_4_12, ExcelType.Number, "Interest 4-12", includeSum: true),
                    items.Col(x => x.CapitalAmount_13_60, ExcelType.Number, "Capital 13-60", includeSum: true),
                    items.Col(x => x.InterestAmount_13_60, ExcelType.Number, "Interest 13-60", includeSum: true),
                    items.Col(x => x.CapitalAmount_61_end, ExcelType.Number, "Capital >=61", includeSum: true),
                    items.Col(x => x.InterestAmount_61_end, ExcelType.Number, "Interest >=61", includeSum: true),
                    items.Col(x => x.NrOfRemainingMonths, ExcelType.Number, "Repayment Time Left in Months", nrOfDecimals: 0),
                    items.Col(x => approximateLastDueDate(x.NrOfRemainingMonths), ExcelType.Date, "Last due date"));

                var client = Service.DocumentClientHttpContext;
                var result = client.CreateXlsx(request);

                return new FileStreamResult(result, XlsxContentType) { FileDownloadName = $"LiquidityExposure_{toDate.ToString("yyyy-MM")}.xlsx" };
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Failed to create liquidity exposure report");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }
    }
}