using nCredit.Code;
using nCredit.Excel;
using NTech.Banking.Shared.Globalization;
using NTech.Services.Infrastructure.Aml;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.WebserviceMethods.Reports.UnsecuredLoansLegacy
{
    public class AmlReportingAidLegacyReportMethod : FileStreamWebserviceMethod<AmlReportingAidLegacyReportMethod.Request>
    {
        public override string Path => "Reports/GetAmlReportingAidLegacy";

        public override bool IsEnabled => IsReportEnabled;

        public static bool IsReportEnabled => NEnv.IsUnsecuredLoansEnabled && !NEnv.IsStandardUnsecuredLoansEnabled && NEnv.ClientCfg.Country.BaseCountry == "FI";

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var forDate = new DateTime(requestContext.Clock().Today.AddYears(-1).Year, 12, 31);

            Dictionary<int, List<string>> creditNrsByCustomerId;

            using (var context = new CreditContext())
            {
                creditNrsByCustomerId = context.CreditHeaders.Select(x => new
                {
                    x.CreditNr,
                    CustomerIds = x.CreditCustomers.Select(y => y.CustomerId),
                    CreditStatus = x
                            .DatedCreditStrings
                            .Where(y => y.Name == DatedCreditStringCode.CreditStatus.ToString() && y.TransactionDate <= forDate)
                            .OrderByDescending(y => y.BusinessEventId)
                            .Select(y => y.Value)
                            .FirstOrDefault()
                })
                .Where(x => x.CreditStatus == CreditStatus.Normal.ToString())
                .SelectMany(x => x.CustomerIds.Select(y => new { CustomerId = y, CreditNr = x.CreditNr }))
                .GroupBy(x => x.CustomerId)
                .ToDictionary(x => x.Key, x => x.Select(y => y.CreditNr).ToList());

                var customerClient = new CreditCustomerClient();
                var customerPropertiesByCustomerId = customerClient.BulkFetchPropertiesByCustomerIdsD(creditNrsByCustomerId.Keys.ToHashSet(),
                    "civicRegNr", "citizencountries", "taxcountries", "addressCountry");

                var sheets = new List<DocumentClientExcelRequest.Sheet>();

                sheets.Add(new DocumentClientExcelRequest.Sheet
                {
                    AutoSizeColumns = true,
                    Title = $"Aml reporting ({forDate.ToString("yyyy-MM-dd")})"
                });
                sheets.Add(new DocumentClientExcelRequest.Sheet
                {
                    AutoSizeColumns = true,
                    Title = "Summary"
                });

                var excelRequest = new DocumentClientExcelRequest
                {
                    Sheets = sheets.ToArray()
                };

                var clientCountry = NEnv.ClientCfg.Country.BaseCountry;

                var euEesCountries = customerClient.LoadSettings("euEesCountryCodes");
                var countryHelper = new NTechCountryHelper(euEesCountries);

                var sharedService = new AmlReportingService(isoCode => countryHelper.IsEuEesMemberState(NTechCountry.FromTwoLetterIsoCode(isoCode, returnNullWhenNotExists: true)), clientCountry);

                var rows = creditNrsByCustomerId.Keys.Select(x => new
                {
                    Customer = sharedService.CreateCustomerModelForFiReporting(x, customerPropertiesByCustomerId.Opt(x) ?? new Dictionary<string, string>()),
                    CreditNrs = creditNrsByCustomerId[x]
                })
                .ToList();

                excelRequest.Sheets[0].SetColumnsAndData(rows,
                    rows.Col(x => x.Customer.CustomerId, ExcelType.Number, "Customer id", isNumericId: true),
                    rows.Col(x => x.Customer.CivicRegNr, ExcelType.Text, "Civic nr"),
                    rows.Col(x => string.Join(", ", x.CreditNrs), ExcelType.Text, "Active accounts"),
                    rows.Col(x => x.Customer.HasTaxCountryFI ? 1 : 0, ExcelType.Number, "FI tax country", nrOfDecimals: 0),
                    rows.Col(x => x.Customer.HasNonFIInsideEuEesTaxCountry ? 1 : 0, ExcelType.Number, "EU/EES tax country", nrOfDecimals: 0),
                    rows.Col(x => x.Customer.HasNonFIOutsideEuEesTaxCountry ? 1 : 0, ExcelType.Number, "Non EU/EES tax country", nrOfDecimals: 0),
                    rows.Col(x => x.Customer.HasNonFICitizenship ? 1 : 0, ExcelType.Number, "Non FI citizen", nrOfDecimals: 0),
                    rows.Col(x => x.Customer.HasNonFIAddressCountry ? 1 : 0, ExcelType.Number, "Non FI address", nrOfDecimals: 0),
                    rows.Col(x => string.Join(", ", x.Customer.TaxCountries), ExcelType.Text, "Tax countries"),
                    rows.Col(x => string.Join(", ", x.Customer.CitizenCountries), ExcelType.Text, "Citizen countries"),
                    rows.Col(x => x.Customer.AddressCountry, ExcelType.Text, "Address country"));

                var summaries = sharedService.ComputeFiQuestionAnswers(rows.Select(x => x.Customer).ToList());

                excelRequest.Sheets[1].SetColumnsAndData(summaries,
                    summaries.Col(x => x.Question, ExcelType.Text, "Question"),
                    summaries.Col(x => x.Count, ExcelType.Number, "Count", nrOfDecimals: 0));

                var client = requestContext.Service().DocumentClientHttpContext;
                var result = client.CreateXlsx(excelRequest);

                return ExcelFile(result, downloadFileName: $"AmlReportingAid-UL-FI-{forDate.ToString("yyyy-MM-dd")}.xlsx");
            }
        }

        public class Request
        {

        }
    }
}