using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace nGccCustomerApplication.Code.ProviderIntegrations
{
    public class LendoIntegration : ProviderIntegrationBase
    {
        private enum MaritalStatus
        {
            Married,
            Cohabitation,
            Single,
            Widowed,
            Registered
        }

        private static string MapMaritalStatus(MaritalStatus s)
        {
            switch (s)
            {
                case MaritalStatus.Married: return "marriage_gift";
                case MaritalStatus.Single: return "marriage_ogift";
                case MaritalStatus.Cohabitation: return "marriage_sambo";
                case MaritalStatus.Widowed: return "marriage_gift";
                case MaritalStatus.Registered: return "marriage_gift";
                default:
                    throw new NotImplementedException();
            }
        }

        private enum EmploymentStatus
        {
            Permanent,
            PartTime,
            Entrepreneur,
            Pensioner,
            DisabilityPensioner,
            Unemployed,
            Student,
            MaternityLeave,
            LimitedTime //Meaning with a fixed end date. Used together with employedUntilDate.
        }

        private static string MapEmploymentStatus(EmploymentStatus s)
        {
            switch (s)
            {
                case EmploymentStatus.Permanent: return "employment_fastanstalld";
                case EmploymentStatus.PartTime: return "employment_visstidsanstalld";
                case EmploymentStatus.Entrepreneur: return "employment_foretagare";
                case EmploymentStatus.Pensioner: return "employment_pensionar";
                case EmploymentStatus.Student: return "employment_studerande";
                case EmploymentStatus.DisabilityPensioner: return "employment_sjukpensionar";
                case EmploymentStatus.Unemployed: return "employment_arbetslos";
                case EmploymentStatus.MaternityLeave: return "employment_fastanstalld";
                case EmploymentStatus.LimitedTime: return "employment_visstidsanstalld";
                default:
                    throw new NotImplementedException();
            }
        }

        private enum EducationStatus
        {
            PrimarySchool,
            Gymnasium,
            VocationalSchool,
            GraduateSchool,
	        HighSchool,
	        ProfessionalSchool,
	        University
        }

        private static string MapEducationStatus(EducationStatus s)
        {
            switch (s)
            {
                case EducationStatus.PrimarySchool: return "education_grundskola";
                case EducationStatus.Gymnasium: return "education_gymnasie";
                case EducationStatus.VocationalSchool: return "education_yrkesskola";
                case EducationStatus.GraduateSchool: return "education_hogskola";
                case EducationStatus.HighSchool: return "education_gymnasie";
                case EducationStatus.ProfessionalSchool: return "education_yrkesskola";
                case EducationStatus.University: return "education_hogskola";
                default:
                    throw new NotImplementedException();
            }
        }

        private enum HousingStatus
        {
            OwnedHouse,
            OwnedApartment,
            RentedApartment,
            Parents,
            CompanyResidence
        }

        private static string MapHousingStatus(HousingStatus s)
        {
            switch (s)
            {
                case HousingStatus.OwnedHouse: return "housing_egenbostad";
                case HousingStatus.OwnedApartment: return "housing_bostadsratt";
                case HousingStatus.RentedApartment: return "housing_hyresbostad";
                case HousingStatus.Parents: return "housing_hosforaldrar";
                case HousingStatus.CompanyResidence: return "housing_tjanstebostad";
                default:
                    throw new NotImplementedException();
            }
        }
        
        protected override Tuple<bool, PreCreditClient.CreditApplicationRequest> DoTranslate()
        {
            const string app = "application";

            Action<string, string, bool> parseApplicant = (externalPrefix, internalPrefix, isCoApplicant) =>
            {
                var applicantNr = isCoApplicant ? 2 : 1;
                OptionalBool($"{externalPrefix}approvedSat", internalPrefix, "approvedSat");
                RequiredCivicRegNumberFi($"{externalPrefix}civicRegNr", internalPrefix, "civicRegNr", _ => internalItems.Add(new PreCreditClient.CreditApplicationRequest.Item { Group = internalPrefix, Name = "civicRegNrCountry", Value = "FI" }));
                RequiredString($"{externalPrefix}phone", internalPrefix, "phone", 100);
                RequiredEmail($"{externalPrefix}email", internalPrefix, "email");
                RequiredEnum<MaritalStatus>($"{externalPrefix}maritalStatus", internalPrefix, "marriage", MapMaritalStatus);
                RequiredInt($"{ externalPrefix}nrOfChildren", internalPrefix, "nrOfChildren");
                RequiredEnum<EmploymentStatus>($"{externalPrefix}employment", internalPrefix, "employment", MapEmploymentStatus);
                RequiredEnum<EducationStatus>($"{externalPrefix}education", internalPrefix, "education", MapEducationStatus);
                RequiredInt($"{externalPrefix}incomePerMonthAmount", internalPrefix, "incomePerMonthAmount");
                RequiredEnum<HousingStatus>($"{externalPrefix}housing", internalPrefix, "housing", MapHousingStatus);
                OptionalString($"{externalPrefix}employer", internalPrefix, "employer", 150);
                OptionalString($"{externalPrefix}employerPhone", internalPrefix, "employerPhone", 100);
                OptionalMonth($"{externalPrefix}employedSinceMonth", internalPrefix, "employedSinceMonth");
                OptionalDate($"{externalPrefix}employedUntilDate", internalPrefix, "employedUntilDate");
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
            RequiredDecimal("amount", app, "amount");
            RequiredInt("repaymentTimeInYears", app, "repaymentTimeInYears");
            OptionalInt("loansToSettleAmount", app, "loansToSettleAmount");
            AllowedButIgnored("iban");
            
            var nrOfApplicants = 1;

            //Applicant 1
            parseApplicant("a1.", "applicant1", false);

            //Applicant 2            
            const string coApplicantExternalPrefix = "a2.";
            if (externalItems.Any(x => x.Name.StartsWith(coApplicantExternalPrefix)))
            {
                nrOfApplicants = 2;
                parseApplicant(coApplicantExternalPrefix, "applicant2", true);
            }

            return Tuple.Create(true, new PreCreditClient.CreditApplicationRequest
            {
                NrOfApplicants = nrOfApplicants,
                ProviderName = ProviderName,
                UserLanguage = "fi",
                Items = internalItems.ToArray()
            });
        }

        public const string ProviderName = "lendo";
    }
}