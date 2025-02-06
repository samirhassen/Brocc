using nCustomerPages.Code;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace nCustomerPages.Controllers.UnsecuredCreditAffiliates.ProviderIntegrations
{
    public class StandardIntegration : ProviderIntegrationBase
    {
        private readonly string providerName;

        //Since we use the internal names in the external model here these messages just get confusing with like x mapping to x everywhere.
        protected override bool IncludeInternalNamesInErrorMessage => false;

        public StandardIntegration(string providerName)
        {
            this.providerName = providerName;
        }

        private enum MaritalStatus
        {
            single, married, cohabitant, divorced, widowed
        }

        private static string MapMaritalStatus(MaritalStatus s)
        {
            switch (s)
            {
                case MaritalStatus.married: return "marriage_gift";
                case MaritalStatus.single: return "marriage_ogift";
                case MaritalStatus.divorced: return "marriage_ogift";
                case MaritalStatus.widowed: return "marriage_ogift";
                case MaritalStatus.cohabitant: return "marriage_sambo";
                default:
                    throw new NotImplementedException();
            }
        }

        private enum EmploymentStatus
        {
            fulltime, own, temp, hourly, project, retired, unemployed, student, parttime, preretired
        }

        private static string MapEmploymentStatus(EmploymentStatus s)
        {
            switch (s)
            {
                case EmploymentStatus.fulltime: return "employment_fastanstalld";
                case EmploymentStatus.own: return "employment_foretagare";
                case EmploymentStatus.temp: return "employment_visstidsanstalld";
                case EmploymentStatus.hourly: return "employment_visstidsanstalld";
                case EmploymentStatus.project: return "employment_visstidsanstalld";
                case EmploymentStatus.retired: return "employment_pensionar";
                case EmploymentStatus.unemployed: return "employment_arbetslos";
                case EmploymentStatus.student: return "employment_studerande";
                case EmploymentStatus.parttime: return "employment_visstidsanstalld";
                case EmploymentStatus.preretired: return "employment_sjukpensionar";
                default:
                    throw new NotImplementedException();
            }
        }

        private enum EducationStatus
        {
            primaryschool, highschool, university, vocationschool
        }

        private static string MapEducationStatus(EducationStatus s)
        {
            switch (s)
            {
                case EducationStatus.primaryschool: return "education_grundskola";
                case EducationStatus.highschool: return "education_gymnasie";
                case EducationStatus.vocationschool: return "education_yrkesskola";
                case EducationStatus.university: return "education_hogskola";
                default:
                    throw new NotImplementedException();
            }
        }

        private enum HousingStatus
        {
            owned, condominium, rentedapartment, withparents, servicehousing
        }

        private static string MapHousingStatus(HousingStatus s)
        {
            switch (s)
            {
                case HousingStatus.servicehousing: return "housing_tjanstebostad";
                case HousingStatus.rentedapartment: return "housing_hyresbostad";
                case HousingStatus.withparents: return "housing_hosforaldrar";
                case HousingStatus.condominium: return "housing_bostadsratt";
                case HousingStatus.owned: return "housing_egenbostad";
                default:
                    throw new NotImplementedException();
            }
        }

        protected override Tuple<bool, CreditApplicationRequest> DoTranslate()
        {
            const string app = "application";

            Action<string, string, bool> parseApplicant = (externalPrefix, internalPrefix, isCoApplicant) =>
            {
                var applicantNr = isCoApplicant ? 2 : 1;
                OptionalBool($"{externalPrefix}approvedSat", internalPrefix, "approvedSat");
                RequiredCivicRegNumberFi($"{externalPrefix}civicRegNr", internalPrefix, "civicRegNr", _ => internalItems.Add(new CreditApplicationRequest.Item { Group = internalPrefix, Name = "civicRegNrCountry", Value = "FI" }));
                RequiredString($"{externalPrefix}phone", internalPrefix, "phone", 100);
                RequiredEmail($"{externalPrefix}email", internalPrefix, "email");
                RequiredEnum<MaritalStatus>($"{externalPrefix}maritalStatus", internalPrefix, "marriage", MapMaritalStatus);
                RequiredInt($"{externalPrefix}nrOfChildren", internalPrefix, "nrOfChildren");
                RequiredEnum<EmploymentStatus>($"{externalPrefix}employment", internalPrefix, "employment", MapEmploymentStatus);
                RequiredEnum<EducationStatus>($"{externalPrefix}education", internalPrefix, "education", MapEducationStatus);
                RequiredInt($"{externalPrefix}incomePerMonthAmount", internalPrefix, "incomePerMonthAmount");
                RequiredEnum<HousingStatus>($"{externalPrefix}housing", internalPrefix, "housing", MapHousingStatus);
                OptionalString($"{externalPrefix}employer", internalPrefix, "employer", 150);
                OptionalString($"{externalPrefix}employerPhone", internalPrefix, "employerPhone", 100);
                OptionalMonth($"{externalPrefix}employedSinceMonth", internalPrefix, "employedSinceMonth");
                OptionalInt($"{externalPrefix}housingCostPerMonthAmount", internalPrefix, "housingCostPerMonthAmount");

                //Other loans
                OptionalInt($"{externalPrefix}creditCardAmount", internalPrefix, "creditCardAmount");
                OptionalInt($"{externalPrefix}creditCardCostPerMonthAmount", internalPrefix, "creditCardCostPerMonthAmount");
                OptionalInt($"{externalPrefix}mortgageLoanAmount", internalPrefix, "mortgageLoanAmount");
                OptionalInt($"{externalPrefix}mortgageLoanCostPerMonthAmount", internalPrefix, "mortgageLoanCostPerMonthAmount");
                OptionalInt($"{externalPrefix}studentLoanAmount", internalPrefix, "studentLoanAmount");
                OptionalInt($"{externalPrefix}studentLoanCostPerMonthAmount", internalPrefix, "studentLoanCostPerMonthAmount");
                OptionalInt($"{externalPrefix}carOrBoatLoanAmount", internalPrefix, "carOrBoatLoanAmount");
                OptionalInt($"{externalPrefix}carOrBoatLoanCostPerMonthAmount", internalPrefix, "carOrBoatLoanCostPerMonthAmount");
                OptionalInt($"{externalPrefix}otherLoanAmount", internalPrefix, "otherLoanAmount");
                OptionalInt($"{externalPrefix}otherLoanCostPerMonthAmount", internalPrefix, "otherLoanCostPerMonthAmount");
            };

            //Application
            AllowedButIgnored("application.externalId"); //Handled elsewhere
            RequiredDecimal("application.amount", app, "amount");
            RequiredInt("application.repaymentTimeInYears", app, "repaymentTimeInYears");
            OptionalInt("application.loansToSettleAmount", app, "loansToSettleAmount");
            OptionalString("application.campaignCode", app, "campaignCode", 100, filterLinebreaks: true);

            var nrOfApplicants = 1;

            //Applicant 1
            parseApplicant("applicant1.", "applicant1", false);

            //Applicant 2            
            const string coApplicantExternalPrefix = "applicant2.";
            if (externalItems.Any(x => x.Name.StartsWith(coApplicantExternalPrefix)))
            {
                nrOfApplicants = 2;
                parseApplicant(coApplicantExternalPrefix, "applicant2", true);
            }

            return Tuple.Create(true, new CreditApplicationRequest
            {
                NrOfApplicants = nrOfApplicants,
                ProviderName = this.providerName,
                UserLanguage = "fi",
                Items = internalItems.ToArray()
            });
        }

        public static ExternalProviderApplicationController.ExternalApplicationRequest TranslateToExternalRequest(StandardRequest request)
        {
            var result = new ExternalProviderApplicationController.ExternalApplicationRequest
            {
                ExternalId = request?.externalId,
                Items = new List<ExternalProviderApplicationController.ExternalApplicationRequest.Item>()
            };

            AddItems(request, "application", result.Items);
            if (request?.applicants != null)
            {
                foreach (var a in request.applicants.Select((x, i) => new { x = x, i = i }))
                {
                    AddItems(a.x, $"applicant{a.i + 1}", result.Items);
                }
            }

            return result;
        }

        private static void AddItems<TSource>(TSource source, string groupName, List<ExternalProviderApplicationController.ExternalApplicationRequest.Item> target)
        {
            Action<string, string> addItem = (name, value) => target.Add(new ExternalProviderApplicationController.ExternalApplicationRequest.Item { Name = name, Value = value });
            Func<string, string> n = x => $"{groupName}.{x}";

            if (source == null)
                return;

            foreach (var p in typeof(TSource).GetProperties())
            {
                var value = p.GetValue(source);
                if (value == null)
                {
                    continue;
                }
                if (p.PropertyType.FullName == typeof(decimal?).FullName)
                {
                    var v = (decimal)p.GetValue(source);
                    addItem(n(p.Name), v.ToString(CultureInfo.InvariantCulture));
                }
                else if (p.PropertyType.FullName == typeof(string).FullName)
                {
                    var v = (string)p.GetValue(source);
                    if (!string.IsNullOrWhiteSpace(v))
                        addItem(n(p.Name), v.ToString(CultureInfo.InvariantCulture));
                }
                else if (p.PropertyType.FullName == typeof(int?).FullName)
                {
                    var v = (int)p.GetValue(source);
                    addItem(n(p.Name), v.ToString(CultureInfo.InvariantCulture));
                }
                else if (p.PropertyType.FullName == typeof(bool?).FullName)
                {
                    var v = (bool?)p.GetValue(source);
                    if (v.HasValue)
                        addItem(n(p.Name), v.Value ? "true" : "false");
                }
            }
        }

        public class StandardRequest
        {
            public string externalId { get; set; }
            public decimal? amount { get; set; }
            public int? repaymentTimeInYears { get; set; }
            public decimal? loansToSettleAmount { get; set; }
            public string campaignCode { get; set; }
            public List<StandardRequestApplicant> applicants { get; set; }
        }

        public class StandardRequestApplicant
        {
            public string civicRegNr { get; set; }
            public string phone { get; set; }
            public string email { get; set; }
            public string maritalStatus { get; set; }
            public int? nrOfChildren { get; set; }
            public string education { get; set; }
            public string housing { get; set; }
            public int? housingCostPerMonthAmount { get; set; }
            public string employment { get; set; }
            public int? incomePerMonthAmount { get; set; }
            public string employer { get; set; }
            public string employedSinceMonth { get; set; }
            public string employerPhone { get; set; }
            public int? creditCardAmount { get; set; }
            public int? creditCardCostPerMonthAmount { get; set; }
            public int? mortgageLoanAmount { get; set; }
            public int? mortgageLoanCostPerMonthAmount { get; set; }
            public int? studentLoanAmount { get; set; }
            public int? studentLoanCostPerMonthAmount { get; set; }
            public int? carOrBoatLoanAmount { get; set; }
            public int? carOrBoatLoanCostPerMonthAmount { get; set; }
            public int? otherLoanAmount { get; set; }
            public int? otherLoanCostPerMonthAmount { get; set; }
            public bool? approvedSat { get; set; }
            public bool? creditReportConsent { get; set; }
            public bool? customerConsent { get; set; }
        }
    }
}