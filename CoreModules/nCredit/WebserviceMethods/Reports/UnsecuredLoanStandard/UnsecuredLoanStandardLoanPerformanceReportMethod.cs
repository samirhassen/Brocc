using nCredit.Code;
using nCredit.DbModel;
using nCredit.Excel;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nCredit.WebserviceMethods.Reports
{
    public class UnsecuredLoanStandardLoanPerformanceReportMethod : FileStreamWebserviceMethod<UnsecuredLoanStandardLoanPerformanceReportMethod.Request>
    {
        public override string Path => "Reports/GetUnsecuredLoanStandardLoanPerformance";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled && NEnv.IsStandardUnsecuredLoansEnabled;

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            Action<List<CreditNotNotifiedInterestRepository.CreditNotNotifiedInterestDetailItem>> setInterestDetails = null;
            List<CreditNotNotifiedInterestRepository.CreditNotNotifiedInterestDetailItem> interestDetails = null;

            if (request.IncludeNotNotifiedInterestDetails.GetValueOrDefault())
            {
                setInterestDetails = x => interestDetails = x;
            }

            var resolver = requestContext.Service();
            var credits = new Shared.LoanPerformanceReportModelService(resolver.ContextFactory,
                    NEnv.EnvSettings, NEnv.GetAffiliateModels, resolver.CalendarDateService, resolver.PaymentOrder).GetCredits(request.Date.Value, null,
                    setInterestDetails: setInterestDetails);

            var sheets = new List<DocumentClientExcelRequest.Sheet>();

            sheets.Add(new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = $"Loan performance ({request.Date.Value.ToString("yyyy-MM-dd")})"
            });

            if (interestDetails != null)
            {
                sheets.Add(new DocumentClientExcelRequest.Sheet
                {
                    AutoSizeColumns = true,
                    Title = $"Interest details"
                });
            }

            var excelRequest = new DocumentClientExcelRequest
            {
                Sheets = sheets.ToArray()
            };

            var s = excelRequest.Sheets[0];
            s.SetColumnsAndData(credits,
                credits.Col(x => x.CreditNr, ExcelType.Text, "Credit nr"),
                credits.Col(x => x.CreationDate, ExcelType.Date, "Creation date"),
                credits.Col(x => x.InitialCapitalBalance, ExcelType.Number, "Initial capital balance"),
                credits.Col(x => x.MarginInterestRate.HasValue && x.ReferenceInterestRate.HasValue ? new decimal?((x.MarginInterestRate.Value + x.ReferenceInterestRate.Value) / 100m) : null, ExcelType.Percent, "Interest rate"),
                credits.Col(x => x.StoredInitialEffectiveInterestRatePercent / 100m, ExcelType.Percent, "Eff. Interest rate"),
                credits.Col(x => x.CapitalBalance, ExcelType.Number, "Capital balance"),
                credits.Col(x => x.NotifiedInterestDebt ?? 0m, ExcelType.Number, "Notified interest balance", includeSum: true),
                credits.Col(x => x.ProviderDisplayName, ExcelType.Text, "Provider"),
                credits.Col(x => x.Status, ExcelType.Text, "Status"),
                credits.Col(x => x.Status == CreditStatus.Normal.ToString() ? new DateTime?() : x.StatusDate, ExcelType.Date, "Closed date"),
                credits.Col(x => x.NrOfOverdueCount, ExcelType.Number, "Overdue count", nrOfDecimals: 0),
                credits.Col(x => x.NrOfOverdueDays, ExcelType.Number, "Days past due date", nrOfDecimals: 0),
                credits.Col(x => x.NotNotifiedInterestBalance, ExcelType.Number, "Not notified interest balance", includeSum: true),
                credits.Col(x => x.TotalInterestBalance, ExcelType.Number, "Notified and not notified interest balance", includeSum: true),
                credits.Col(x => x.NotfiedBalance, ExcelType.Number, "Total unpaid notified", includeSum: true)
            );


            if (interestDetails != null)
            {
                var s2 = excelRequest.Sheets[1];
                s2.SetColumnsAndData(interestDetails,
                    interestDetails.Col(x => x.CreditNr, ExcelType.Text, "CreditNr"),
                    interestDetails.Col(x => x.CurrentCreditNextInterestFromDate, ExcelType.Date, "CurrentCreditNextInterestFromDate"),
                    interestDetails.Col(x => x.BlockFromDate, ExcelType.Date, "BlockFromDate"),
                    interestDetails.Col(x => x.BlockToDate, ExcelType.Date, "BlockToDate"),
                    interestDetails.Col(x => x.BlockInterestAmount, ExcelType.Number, "BlockInterestAmount", includeSum: true),
                    interestDetails.Col(x => x.CapitalDebtAmount, ExcelType.Number, "CapitalDebtAmount"),
                    interestDetails.Col(x => x.InterestRate, ExcelType.Number, "InterestRate"),
                    interestDetails.Col(x => x.DayInterestAmount, ExcelType.Number, "DayInterestAmount"),
                    interestDetails.Col(x => x.NrOfDaysInBlock, ExcelType.Number, "NrOfDaysInBlock", nrOfDecimals: 0, includeSum: true));
            }

            var client = requestContext.Service().DocumentClientHttpContext;
            var result = client.CreateXlsx(excelRequest);

            return ExcelFile(result, downloadFileName: $"LoanPerformance-{request.Date.Value.ToString("yyyy-MM-dd")}.xlsx");
        }

        public class Request
        {
            [Required]
            public DateTime? Date { get; set; }
            public bool? IncludeNotNotifiedInterestDetails { get; set; }
        }
    }

}
