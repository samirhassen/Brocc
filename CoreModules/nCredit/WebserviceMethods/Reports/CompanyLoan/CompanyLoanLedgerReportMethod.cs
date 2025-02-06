using nCredit.Code;
using nCredit.Excel;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;

namespace nCredit.WebserviceMethods.Reports
{
    public class CompanyLoanLedgerReportMethod : FileStreamWebserviceMethod<nCredit.WebserviceMethods.Reports.Shared.Credits.Request>
    {
        public override string Path => "Reports/GetCompanyLoanLedger";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, nCredit.WebserviceMethods.Reports.Shared.Credits.Request request)
        {
            Validate(request, x =>
            {
                x.Require(y => y.Date);
            });

            var credits = nCredit.WebserviceMethods.Reports.Shared.Credits.GetCredits(request);

            var sheets = new List<DocumentClientExcelRequest.Sheet>();

            sheets.Add(new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = $"CompanyLoan Ledger ({request.Date.Value.ToString("yyyy-MM-dd")})"
            });

            var excelRequest = new DocumentClientExcelRequest
            {
                Sheets = sheets.ToArray()
            };

            var s = excelRequest.Sheets[0];
            s.SetColumnsAndData(credits,
                credits.Col(x => x.CreditNr, ExcelType.Text, "Credit nr"),
                credits.Col(x => x.CompanyName, ExcelType.Text, "Company name"),
                NEnv.ClientCfg.Country.BaseCountry == "SE"
                    ? credits.Col(x => x.CompanyLoanSniKodSe, ExcelType.Text, "Snikod")
                    : null,
                credits.Col(x => x.InitalCapitalDebt, ExcelType.Number, "Inital capital debt", nrOfDecimals: 2),
                credits.Col(x => x.CreationDate, ExcelType.Date, "Creation date"),
                credits.Col(x => x.InitialPaymentFileDate, ExcelType.Date, "Initial payment date"),
                credits.Col(x => x.InitialPaymentFileAmount, ExcelType.Number, "Initial payment amount"),
                credits.Col(x => x.InitialRemainingModel.LastPaymentDate, ExcelType.Date, "Initial end date"),
                credits.Col(x => x.CurrentRemainingModel.LastPaymentDate, ExcelType.Date, "Current end date"),
                credits.Col(x => x.Status, ExcelType.Text, "Status"),
                credits.Col(x => x.Status == CreditStatus.Normal.ToString() ? new DateTime?() : x.StatusDate, ExcelType.Date, "Closed date"),
                credits.Col(x => x.CapitalDebt, ExcelType.Number, "Capital debt"),
                credits.Col(x => x.MarginInterestRate.HasValue && x.ReferenceInterestRate.HasValue ? new decimal?((x.MarginInterestRate.Value + x.ReferenceInterestRate.Value) / 100m) : null, ExcelType.Percent, "Interest rate"),
                credits.Col(x => x.TotalNotifiedUnpaidBalance ?? 0m, ExcelType.Number, "Total unpaid notified", includeSum: true),
                credits.Col(x => x.TotalPaidInterest, ExcelType.Number, "Total paid interest ", nrOfDecimals: 2),
                credits.Col(x => x.TotalNotifiedCapital, ExcelType.Number, "Total notified capital", nrOfDecimals: 2),
                credits.Col(x => x.TotalNotifiedFees, ExcelType.Number, "Total notified fees", nrOfDecimals: 2),
                credits.Col(x => x.TotalNotifiedInterest, ExcelType.Number, "Total notified interest", nrOfDecimals: 2),
                credits.Col(x => x.TotalPaidFees, ExcelType.Number, "Total paid fees", nrOfDecimals: 2)
                );

            var client = requestContext.Service().DocumentClientHttpContext;
            var result = client.CreateXlsx(excelRequest);

            return ExcelFile(result, downloadFileName: $"CompanyLoanLedger-{request.Date.Value.ToString("yyyy-MM-dd")}.xlsx");
        }
    }

}
