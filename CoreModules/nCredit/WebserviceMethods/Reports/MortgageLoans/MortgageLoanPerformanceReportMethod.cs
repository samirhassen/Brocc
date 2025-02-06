using nCredit.Code;
using nCredit.DbModel;
using nCredit.Excel;
using nCredit.WebserviceMethods.Reports.Shared;
using NTech;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.WebserviceMethods.Reports
{
    public class MortgageLoanPerformanceReportMethod : FileStreamWebserviceMethod<MortgageLoanPerformanceReportMethod.Request>
    {
        public override string Path => "Reports/GetMortgageLoanPerformance";

        public override bool IsEnabled => NEnv.IsMortgageLoansEnabled;

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, x =>
            {
                x.Require(y => y.Date);
            });

            using (var context = new CreditContext())
            {
                List<CreditNotNotifiedInterestRepository.CreditNotNotifiedInterestDetailItem> interestDetails = null;
                Action<List<CreditNotNotifiedInterestRepository.CreditNotNotifiedInterestDetailItem>> setInterestDetails = null;

                if (request.IncludeNotNotifiedInterestDetails.GetValueOrDefault())
                {
                    setInterestDetails = x => interestDetails = x;
                }

                var resolver = requestContext.Service();
                var credits = new LoanPerformanceReportModelService(resolver.ContextFactory,
                    NEnv.EnvSettings, NEnv.GetAffiliateModels, resolver.CalendarDateService, resolver.PaymentOrder).GetCredits(request.Date.Value, null,
                    onlyThisCreditNr: request.CreditNr, setInterestDetails: setInterestDetails);

                var sheets = new List<DocumentClientExcelRequest.Sheet>();

                sheets.Add(new DocumentClientExcelRequest.Sheet
                {
                    AutoSizeColumns = true,
                    Title = $"Mortgageloan performance ({request.Date.Value.ToString("yyyy-MM-dd")})"
                });

                var excelRequest = new DocumentClientExcelRequest
                {
                    Sheets = sheets.ToArray()
                };

                int? GetDaysUntilRebinding(DateTime? nextInterestRebindDate)
                {
                    if (!nextInterestRebindDate.HasValue)
                        return null;
                    if (nextInterestRebindDate.Value <= request.Date.Value)
                        return 0;
                    return Dates.GetAbsoluteNrOfDaysBetweenDates(request.Date.Value, nextInterestRebindDate.Value);
                }

                var s = excelRequest.Sheets[0];
                s.SetColumnsAndData(credits, Enumerables.SkipNulls(
                    credits.Col(x => x.CreditNr, ExcelType.Text, "Credit nr"),
                    NEnv.ClientCfg.Country.BaseCountry == "FI" ? credits.Col(x => x.IsForNonPropertyUse ? "Mortgage consumer loan" : "Mortgage loan", ExcelType.Text, "Loan type") : null,
                    credits.Col(x => x.CreationDate, ExcelType.Date, "Creation date"),
                    NEnv.ClientCfg.Country.BaseCountry == "SE" ? credits.Col(x => x.MortgageLoanInitialSettlementDate ?? x.CreationDate, ExcelType.Date, "Settlement date") : null,
                    credits.Col(x => x.InitialCapitalBalance, ExcelType.Number, "Initial capital balance"),
                    credits.Col(x => x.MarginInterestRate.HasValue && x.ReferenceInterestRate.HasValue ? new decimal?((x.MarginInterestRate.Value + x.ReferenceInterestRate.Value) / 100m) : null, ExcelType.Percent, "Interest rate"),
                    credits.Col(x => x.ReferenceInterestRate.HasValue ? new decimal?(x.ReferenceInterestRate.Value / 100m) : null, ExcelType.Percent, "Reference interest rate"),
                    credits.Col(x => x.MarginInterestRate.HasValue ? new decimal?(x.MarginInterestRate.Value / 100m) : null, ExcelType.Percent, "Margin interest rate"),
                    credits.Col(x => x.BookKeepingCapitalBalance, ExcelType.Number, "Capital balance"),
                    credits.Col(x => x.NotifiedInterestDebt ?? 0m, ExcelType.Number, "Notified unpaid interest balance", includeSum: true),
                    credits.Col(x => x.PaidInterestAmountTotal, ExcelType.Number, "Interest revenue", includeSum: true),
                    credits.Col(x => x.ProviderDisplayName, ExcelType.Text, "Provider"),
                    credits.Col(x => x.Status, ExcelType.Text, "Status"),
                    credits.Col(x => x.Status == CreditStatus.Normal.ToString() ? new DateTime?() : x.StatusDate, ExcelType.Date, "Closed date"),
                    credits.Col(x => x.NrOfOverdueCount, ExcelType.Number, "Overdue count", nrOfDecimals: 0),
                    credits.Col(x => x.NrOfOverdueDays, ExcelType.Number, "Days past due date", nrOfDecimals: 0),
                    credits.Col(x => x.NotNotifiedInterestBalance, ExcelType.Number, "Not notified interest balance", includeSum: true),
                    credits.Col(x => x.TotalInterestBalance, ExcelType.Number, "Notified and not notified interest balance", includeSum: true),
                    credits.Col(x => x.NotfiedBalance, ExcelType.Number, "Total unpaid notified", includeSum: true),
                    NEnv.ClientCfg.Country.BaseCountry == "FI" ? credits.Col(x => x.AnnuityAmount, ExcelType.Number, "Annuity") : null,
                    credits.Col(x => x.NotificationFee, ExcelType.Number, "Notification fee"),
                    NEnv.HasPerLoanDueDay ? credits.Col(x => x.NotificationDueDay, ExcelType.Number, "Due day", isNumericId: true) : null,
                    NEnv.ClientCfg.Country.BaseCountry == "FI" ? credits.Col(x => (x.ConnectedCreditNrs != null && x.ConnectedCreditNrs.Count > 0) ? string.Join(", ", x.ConnectedCreditNrs) : null, ExcelType.Text, "Connected credits") : null,
                    credits.Col(x => x.MortgageLoanInterestRebindMonthCount, ExcelType.Number, "Binding period (months)", nrOfDecimals: 0),
                    credits.Col(x => x.MortgageLoanNextInterestRebindDate, ExcelType.Date, "Next rebinding date"),
                    credits.Col(x => GetDaysUntilRebinding(x.MortgageLoanNextInterestRebindDate), ExcelType.Number, "Days until next rebinding", nrOfDecimals: 0),
                    credits.Col(x => x.MortgageLoanOwner == "[none]" ? null : x.MortgageLoanOwner, ExcelType.Text, "Loan owner")
                ).ToArray());

                var client = requestContext.Service().DocumentClientHttpContext;
                var result = client.CreateXlsx(excelRequest);

                return ExcelFile(result, downloadFileName: $"MortgageloanPerformance-{request.Date.Value.ToString("yyyy-MM-dd")}.xlsx");
            }
        }

        public class Request
        {
            public DateTime? Date { get; set; }
            public bool? IncludeNotNotifiedInterestDetails { get; set; }
            public string CreditNr { get; set; }
            public bool? IncludeActualOverdue { get; set; }
        }
    }
}