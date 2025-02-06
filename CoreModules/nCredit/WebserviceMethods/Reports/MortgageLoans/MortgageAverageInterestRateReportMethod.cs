using nCredit.Code;
using nCredit.Excel;
using NTech.Core.Credit.Shared.Services;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;

namespace nCredit.WebserviceMethods.Reports.MortgageLoans
{
    public class MortgageAverageInterestRateReportMethod : FileStreamWebserviceMethod<MortgageAverageInterestRateReportMethod.Request>
    {
        public override string Path => "Reports/MortgageAverageInterestRates";

        public override bool IsEnabled => IsReportEnabled;
        public static bool IsReportEnabled => NEnv.IsStandardMortgageLoansEnabled;

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var service = new MortgageLoanAverageInterstRateReportService(requestContext.Service().ContextFactory);

            var reportData = service.GetAverageInterestRates(request.Date.Value);

            var sheets = new List<DocumentClientExcelRequest.Sheet>();
            var translate = GetTranslator(request.Language);

            sheets.Add(new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = $"{translate("average_rates_tab_header")} {request.Date.Value.ToString("yyyy-MM")}"
            });
            sheets[0].SetColumnsAndData(reportData.AverageRates,
                reportData.AverageRates.Col(x => x.RebindMonthCount, ExcelType.Number, translate("rebind_col_header"), nrOfDecimals: 0),
                reportData.AverageRates.Col(x => x.AverageInterestRatePercent / 100m, ExcelType.Percent, translate("avg_rate_col_header")));

            if (request.IncludeDetails == true)
            {
                sheets.Add(new DocumentClientExcelRequest.Sheet
                {
                    AutoSizeColumns = true,
                    Title = translate("credits_tab_header")
                });
                sheets[1].SetColumnsAndData(reportData.AllIncludedCredits,
                    reportData.AllIncludedCredits.Col(x => x.RebindMonthCount, ExcelType.Number, translate("rebind_col_header"), nrOfDecimals: 0),
                    reportData.AllIncludedCredits.Col(x => x.CreditNr, ExcelType.Text, translate("creditnr_col_header")),
                    reportData.AllIncludedCredits.Col(x => x.CapitalBalance, ExcelType.Number, translate("capital_col_header")),
                    reportData.AllIncludedCredits.Col(x => x.InterestRatePercent / 100m, ExcelType.Percent, translate("rate_col_header")));
            }

            var excelRequest = new DocumentClientExcelRequest
            {
                Sheets = sheets.ToArray()
            };

            var client = requestContext.Service().DocumentClientHttpContext;
            var result = client.CreateXlsx(excelRequest);

            return ExcelFile(result, downloadFileName: $"{translate("report_name")}-{request.Date.Value.ToString("yyyy-MM")}.xlsx");

        }

        private static Func<string, string> GetTranslator(string language)
        {
            if (language == "local")
            {
                language = NEnv.ClientCfgCore.Country.GetBaseLanguage();
            }
            var d = (language == "sv" ? SwedishTranslations : EnglishTranslations);
            return name => d.Opt(name) ?? name;
        }

        private static Dictionary<string, string> SwedishTranslations = new Dictionary<string, string>
        {
            { "average_rates_tab_header", "Snitträntor" },
            { "rebind_col_header", "Bindningstid i månader" },
            { "avg_rate_col_header", "Snittränta" },
            { "credits_tab_header", "Låndetaljer" },
            { "creditnr_col_header", "Lånenr" },
            { "capital_col_header", "Kapitalskuld" },
            { "rate_col_header", "Ränta" },
            { "report_name", "Snitträntor" }
        };
        private static Dictionary<string, string> EnglishTranslations = new Dictionary<string, string>
        {
            { "average_rates_tab_header", "Average interest rates" },
            { "rebind_col_header", "Fixed interest time in months" },
            { "avg_rate_col_header", "Average interest rate" },
            { "credits_tab_header", "Loan details" },
            { "creditnr_col_header", "Loan nr" },
            { "capital_col_header", "Capital balance" },
            { "rate_col_header", "Interest rate" },
            { "report_name", "Average interest rates" }
        };

        public class Request
        {
            public DateTime? Date { get; set; }

            // We exclude this by default unlike all other reports since the use-case here is most likely that the client publishes this unedited on their website
            public bool? IncludeDetails { get; set; }

            // We have language here unlike all other reports since the use-case here is most likely that the client publishes this unedited on their website
            /// <summary>
            /// Report language like "sv" or "en" or "local" to specify using the clients language.
            /// </summary>
            public string Language { get; set; }
        }
    }
}