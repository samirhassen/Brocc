using nCredit.Code;
using nCredit.Excel;
using NTech;
using NTech.Banking.Shared.Globalization;
using NTech.Services.Infrastructure.Aml;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.WebserviceMethods.Reports.CompanyLoan
{
    public class AmlReportingAidReportCompanySeMethod : FileStreamWebserviceMethod<AmlReportingAidReportCompanySeMethod.Request>
    {
        public override string Path => "Reports/GetAmlReportingAidCompanySe";

        public override bool IsEnabled => IsReportEnabled;

        public static bool IsReportEnabled => NEnv.IsCompanyLoansEnabled && NEnv.ClientCfg.Country.BaseCountry == "SE";

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var forDate = request.Date?.Date ?? new DateTime(requestContext.Clock().Today.AddYears(-1).Year, 12, 31);

            using (var context = new CreditContext())
            {
                var dataByCustomerId = context
                    .CreditHeaders.Select(x => new
                    {
                        x.CreditNr,
                        CompanyCustomerIds = x.CreditCustomers.Select(y => y.CustomerId),
                        BeneficialOwnerCustomerIds = x
                                            .CustomerListMembers
                                            .Where(y => y.ListName == "companyLoanBeneficialOwner")
                                            .Select(y => y.CustomerId),
                        CreditStatus = x
                                                .DatedCreditStrings
                                                .Where(y => y.Name == DatedCreditStringCode.CreditStatus.ToString() && y.TransactionDate <= forDate)
                                                .OrderByDescending(y => y.BusinessEventId)
                                                .Select(y => y.Value)
                                                .FirstOrDefault()
                    })
                    .Where(x => x.CreditStatus == CreditStatus.Normal.ToString())
                    .SelectMany(x => x
                        .CompanyCustomerIds.Select(y => new { CustomerId = y, IsCompany = true, CreditNr = x.CreditNr, IsBeneficialOwner = false })
                        .Concat(x.BeneficialOwnerCustomerIds.Select(y => new { CustomerId = y, IsCompany = false, CreditNr = x.CreditNr, IsBeneficialOwner = true })))
                    .GroupBy(x => x.CustomerId)
                    .ToDictionary(x => x.Key, x => new
                    {
                        CreditNrs = x.Select(y => y.CreditNr).ToList(),
                        IsBeneficialOwner = x.Any(y => y.IsBeneficialOwner)
                    });

                var customerClient = new CreditCustomerClient();
                var customerPropertiesByCustomerId = customerClient.BulkFetchPropertiesByCustomerIdsD(dataByCustomerId.Keys.ToHashSet(),
                    "civicRegNr", "orgnr", "citizencountries", "taxcountries", "addressCountry", "isCompany", "amlRiskClass", "localIsPep");

                var kycAnswerDateByCustomerId = GetLatestKycQuestionsAnswerDatePerCustomer(
                    dataByCustomerId.Where(x => x.Value.IsBeneficialOwner).Select(x => x.Key).ToHashSet(), customerClient);

                var sheets = new List<DocumentClientExcelRequest.Sheet>();

                sheets.Add(new DocumentClientExcelRequest.Sheet
                {
                    AutoSizeColumns = true,
                    Title = $"Aml rapportering ({forDate.ToString("yyyy-MM-dd")})"
                });
                sheets.Add(new DocumentClientExcelRequest.Sheet
                {
                    AutoSizeColumns = true,
                    Title = "Sammanställning"
                });

                var excelRequest = new DocumentClientExcelRequest
                {
                    Sheets = sheets.ToArray()
                };

                var clientCountry = NEnv.ClientCfg.Country.BaseCountry;

                var euEesCountries = customerClient.LoadSettings("euEesCountryCodes");
                var countryHelper = new NTechCountryHelper(euEesCountries);

                bool isEuEesMemberState(string countryCode) => countryHelper.IsEuEesMemberState(NTechCountry.FromTwoLetterIsoCode(countryCode, returnNullWhenNotExists: true));

                var sharedService = new AmlReportingService(isEuEesMemberState, clientCountry);

                var rows = dataByCustomerId.Keys.Select(x => new
                {
                    Customer = CreateCustomerModelForSeReporting(x, dataByCustomerId[x].IsBeneficialOwner,
                        customerPropertiesByCustomerId.Opt(x) ?? new Dictionary<string, string>(), kycAnswerDateByCustomerId,
                        clientCountry, sharedService, dataByCustomerId[x].CreditNrs, forDate, isEuEesMemberState),
                    CreditNrs = dataByCustomerId[x].CreditNrs
                })
                .ToList();

                excelRequest.Sheets[0].SetColumnsAndData(rows,
                    rows.Col(x => x.Customer.CustomerId, ExcelType.Number, "Kund id", isNumericId: true),
                    rows.Col(x => x.Customer.CivicOrOrgNr, ExcelType.Text, "Person/orgnr"),
                    rows.Col(x => string.Join(", ", x.CreditNrs), ExcelType.Text, "Aktiva konton"),
                    rows.Col(x => x.Customer.IsPhysicalPerson ? 1 : 0, ExcelType.Number, "Fysisk person", nrOfDecimals: 0),
                    rows.Col(x => x.Customer.IsCompany ? 1 : 0, ExcelType.Number, "Juridisk person", nrOfDecimals: 0),
                    rows.Col(x => x.Customer.IsBeneficialOwner ? 1 : 0, ExcelType.Number, "Verklig huvudman", nrOfDecimals: 0),
                    rows.Col(x => x.Customer.HasTaxCountrySE ? 1 : 0, ExcelType.Number, "SE skatteland", nrOfDecimals: 0),
                    rows.Col(x => x.Customer.HasNonSEInsideEuEesTaxCountry ? 1 : 0, ExcelType.Number, "EU/EES skatteland", nrOfDecimals: 0),
                    rows.Col(x => x.Customer.HasNonSEOutsideEuEesTaxCountry ? 1 : 0, ExcelType.Number, "Ej EU/EES skatteland", nrOfDecimals: 0),
                    rows.Col(x => x.Customer.RiskCategory, ExcelType.Text, "Riskkategori"),
                    rows.Col(x => x.Customer.LatestKycQuestionsAnswerDate, ExcelType.Date, "Kyc svarsdatum"),
                    rows.Col(x => x.Customer.LatestKycQuestionsAnswerAgeInMonths, ExcelType.Number, "Kyc svarsålder i månader", nrOfDecimals: 0),
                    rows.Col(x => x.Customer.IsPep.HasValue ? new int?(x.Customer.IsPep.Value ? 1 : 0) : null, ExcelType.Number, "Pep", nrOfDecimals: 0),
                    rows.Col(x => string.Join(", ", x.Customer.TaxCountries), ExcelType.Text, "Skatteländer"),
                    rows.Col(x => string.Join(", ", x.Customer.CitizenCountries), ExcelType.Text, "Medborgarländer"));

                var summaries = ComputeQuestionAnswers(rows.Select(x => x.Customer).ToList());

                excelRequest.Sheets[1].SetColumnsAndData(summaries,
                    summaries.Col(x => x.Question, ExcelType.Text, "Fråga"),
                    summaries.Col(x => x.Count, ExcelType.Number, "Antal", nrOfDecimals: 0),
                    summaries.Col(x => x.Text, ExcelType.Text, "Beskrivning"));

                var client = requestContext.Service().DocumentClientHttpContext;
                var result = client.CreateXlsx(excelRequest);

                return ExcelFile(result, downloadFileName: $"AmlReportingAid-CL-SE-{forDate.ToString("yyyy-MM-dd")}.xlsx");
            }
        }

        private List<(string Question, int? Count, string Text)> ComputeQuestionAnswers(List<SeReportingCustomerModel> customers)
        {
            var summaries = new List<(string Question, int? Count, string Text)>();
            void A(string name, int? value = null, string text = null) => summaries.Add((name, value, text));
            A("C1 Antal etablerade affärsförbindelser i Sverige", customers.SelectMany(x => x.CreditNrs).Distinct().Count());

            A("Antal kunder med skatterättslig hemvist i Sverige");
            A("C2 Fysiska personer, antal", customers.Count(x => x.IsPhysicalPerson && x.HasTaxCountrySE));
            A("C3 Juridiska personer, antal", customers.Count(x => x.IsCompany && x.HasTaxCountrySE));

            A("Antal kunder med skatterättslig hemvist i EU/EES (ej Sverige)");
            A("C4 Fysiska personer, antal", customers.Count(x => x.IsPhysicalPerson && x.HasNonSEInsideEuEesTaxCountry));
            A("C5 Juridiska personer, antal", customers.Count(x => x.IsCompany && x.HasNonSEInsideEuEesTaxCountry));

            A("Antal kunder med skatterättslig hemvist utanför EU / EES");
            A("C6 Fysiska personer, antal", customers.Count(x => x.IsPhysicalPerson && x.HasNonSEOutsideEuEesTaxCountry));
            A("C7 Juridiska personer, antal", customers.Count(x => x.IsCompany && x.HasNonSEOutsideEuEesTaxCountry));

            A("Hur många kunder bedöms som hög risk? Ange antal");
            A("C8 Fysiska personer, antal", text: "Handräknas från Fysisk person = 1 + Riskkategori");
            A("C9 Juridiska personer, antal", text: "Handräknas från Juridisk person = 1 + Riskkategori");

            A("För hur stort antal av företagets etablerade affärsförbindelser saknas aktuella och tillräckliga uppgifter för kundkännedom?");
            A("C10 Avseende fysiska personer, antal", text: "Handräknas med Fysisk person = 1 och Kyc svarsålder i månader");
            A("C11 Avseende juridiska personer, antal", text: "Handräknas med Fysisk person = 0 och Kyc svarsålder i månader");

            A("Antal kunder som är identifierade som PEP, familjemedlem till PEP eller känd medarbetare till PEP");
            A("C12 Fysiska personer, antal med skatterättslig hemvist i Sverige", customers.Count(x => x.IsPhysicalPerson && x.IsPep == true && x.HasTaxCountrySE));
            A("C13 Verkliga huvudmän för juridiska personer, antal med skatterättslig hemvist i Sverige", customers.Count(x => x.IsBeneficialOwner && x.IsPep == true && x.HasTaxCountrySE));

            A("C14 Fysiska personer, antal med skatterättslig hemvist i EU / EES(ej Sverige)", customers.Count(x => x.IsPhysicalPerson && !x.IsBeneficialOwner && x.IsPep == true && x.HasNonSEInsideEuEesTaxCountry));
            A("C15 Verkliga huvudmän för juridiska personer, antal med skatterättslig hemvist i EU / EES(ej Sverige)", customers.Count(x => x.IsBeneficialOwner && x.IsPep == true && x.HasNonSEInsideEuEesTaxCountry));

            return summaries;
        }

        private Dictionary<int, DateTime?> GetLatestKycQuestionsAnswerDatePerCustomer(ISet<int> customerIds, CreditCustomerClient customerClient)
        {
            var result = new Dictionary<int, DateTime?>();
            foreach (var customerIdGroup in customerIds.ToArray().SplitIntoGroupsOfN(500))
            {
                var groupResult = customerClient.FetchCustomerOnboardingStatuses(customerIds, null, null, false);
                foreach (var customerId in customerIdGroup)
                    result[customerId] = groupResult.Opt(customerId)?.LatestKycQuestionsAnswerDate ?? groupResult.Opt(customerId)?.LatestPropertyUpdateDate;
            }
            return result;
        }

        public class Request
        {
            public DateTime? Date { get; set; }
        }

        private SeReportingCustomerModel CreateCustomerModelForSeReporting(int customerId, bool isBeneficialOwner, Dictionary<string, string> customerProperties,
            Dictionary<int, DateTime?> kycAnswerDateByCustomerId, string clientCountry, AmlReportingService amlReportingService,
            List<string> creditNrs, DateTime forDate, Func<string, bool> isEuEesMemberState)
        {
            var isCompany = amlReportingService.GetProperty("isCompany", customerProperties) == "true";

            var taxCountries = amlReportingService.ParseCountriesArray("taxcountries", customerProperties);
            var citizenCountries = amlReportingService.ParseCountriesArray("citizencountries", customerProperties);
            var addressCountry = (amlReportingService.GetProperty("addressCountry", customerProperties) ?? clientCountry)?.ToUpperInvariant();

            //Use latest stored kyc answers if available or fall back to latest create loan if there are none
            var latestKycQuestionsAnswerDate = kycAnswerDateByCustomerId.ContainsKey(customerId)
                ? kycAnswerDateByCustomerId[customerId]
                : null;

            int? latestKycQuestionsAnswerAgeInMonths = null;
            if (latestKycQuestionsAnswerDate.HasValue)
            {
                if (latestKycQuestionsAnswerDate.Value >= forDate)
                    latestKycQuestionsAnswerAgeInMonths = 0;
                else
                    latestKycQuestionsAnswerAgeInMonths = Dates.GetAbsoluteNrOfMonthsBetweenDates(forDate, latestKycQuestionsAnswerDate.Value);
            }


            var m = new SeReportingCustomerModel
            {
                CustomerId = customerId,
                IsCompany = isCompany,
                IsPhysicalPerson = !isCompany && !isBeneficialOwner,
                IsBeneficialOwner = isBeneficialOwner,
                CivicOrOrgNr = amlReportingService.GetProperty(isCompany ? "orgnr" : "civicRegNr", customerProperties),
                AddressCountry = addressCountry,
                TaxCountries = taxCountries.ToHashSet(),
                CitizenCountries = citizenCountries.ToHashSet(),
                RiskCategory = amlReportingService.GetProperty("amlRiskClass", customerProperties).NormalizeNullOrWhitespace(),
                IsPep = isCompany ? new bool?() : (amlReportingService.GetProperty("localIsPep", customerProperties) == "true"),
                HasTaxCountrySE = taxCountries.Contains(clientCountry),
                HasNonSECitizenship = citizenCountries.Any(x => x != clientCountry),
                HasNonSEInsideEuEesTaxCountry = taxCountries.Any(x => x != clientCountry && isEuEesMemberState(x)),
                HasNonSEOutsideEuEesTaxCountry = taxCountries.Any(x => x != clientCountry && !isEuEesMemberState(x)),
                HasNonSEAddressCountry = addressCountry != clientCountry,
                LatestKycQuestionsAnswerDate = latestKycQuestionsAnswerDate,
                LatestKycQuestionsAnswerAgeInMonths = latestKycQuestionsAnswerAgeInMonths,
                CreditNrs = creditNrs
            };

            return m;
        }

        private class SeReportingCustomerModel
        {
            public int CustomerId { get; set; }
            public string CivicOrOrgNr { get; set; }
            public bool IsCompany { get; set; }
            public bool IsPhysicalPerson { get; set; }
            public bool IsBeneficialOwner { get; set; }
            public bool HasTaxCountrySE { get; set; }
            public bool HasNonSEInsideEuEesTaxCountry { get; set; }
            public bool HasNonSEOutsideEuEesTaxCountry { get; set; }
            public bool HasNonSECitizenship { get; set; }
            public bool HasNonSEAddressCountry { get; set; }
            public string AddressCountry { get; set; }
            public ISet<string> TaxCountries { get; set; }
            public ISet<string> CitizenCountries { get; set; }
            public string RiskCategory { get; set; }
            public bool? IsPep { get; set; }
            public DateTime? LatestKycQuestionsAnswerDate { get; set; }
            public int? LatestKycQuestionsAnswerAgeInMonths { get; set; }
            public List<string> CreditNrs { get; set; }
        }
    }
}