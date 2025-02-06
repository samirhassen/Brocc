using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace nGccCustomerApplication.Code.ProviderIntegrations
{
    public class EtuaIntegration : ProviderIntegrationBase
    {
        private enum EtuaMaritalStatus
        {
            Married,
            Cohabitation,
            Single
        }

        private static string MapEtuaMaritalStatus(EtuaMaritalStatus s)
        {
            switch (s)
            {
                case EtuaMaritalStatus.Cohabitation: return "marriage_sambo";
                case EtuaMaritalStatus.Single: return "marriage_ogift";
                case EtuaMaritalStatus.Married: return "marriage_gift";
                default:
                    throw new NotImplementedException();
            }
        }

        private enum EtuaEmploymentStatus
        {
            Permanent,
            PartTime,
            Entrepreneur,
            Pensioner,
            DisabilityPensioner,
            Unemployed,
            Student
        }

        private static string MapEtuaEmploymentStatus(EtuaEmploymentStatus s)
        {
            switch (s)
            {
                case EtuaEmploymentStatus.Permanent: return "employment_fastanstalld";
                case EtuaEmploymentStatus.PartTime: return "employment_visstidsanstalld";
                case EtuaEmploymentStatus.Entrepreneur: return "employment_foretagare";
                case EtuaEmploymentStatus.Pensioner: return "employment_pensionar";
                case EtuaEmploymentStatus.DisabilityPensioner: return "employment_sjukpensionar";
                case EtuaEmploymentStatus.Unemployed: return "employment_arbetslos";
                case EtuaEmploymentStatus.Student: return "employment_studerande";
                default:
                    throw new NotImplementedException();
            }
        }

        private enum EtuaEducationStatus
        {
            PrimarySchool,
            Gymnasium,
            VocationalSchool,
            GraduateSchool,
            Other
        }
        private static string MapEtuaEducationStatus(EtuaEducationStatus s)
        {
            switch (s)
            {
                case EtuaEducationStatus.PrimarySchool: return "education_grundskola";
                case EtuaEducationStatus.Gymnasium: return "education_gymnasie";
                case EtuaEducationStatus.VocationalSchool: return "education_yrkesskola";
                case EtuaEducationStatus.GraduateSchool: return "education_hogskola";
                case EtuaEducationStatus.Other: return "education_grundskola";
                default:
                    throw new NotImplementedException();
            }
        }

        private enum EtuaHousingStatus
        {
            OwnedHouse,
            OwnedApartment,
            RentedApartment,
            Parents,
            CompanyResidence,
            Other
        }

        private static string MapEtuaHousingStatus(EtuaHousingStatus s)
        {
            switch (s)
            {
                case EtuaHousingStatus.OwnedHouse: return "housing_egenbostad";
                case EtuaHousingStatus.OwnedApartment: return "housing_bostadsratt";
                case EtuaHousingStatus.RentedApartment: return "housing_hyresbostad";
                case EtuaHousingStatus.Parents: return "housing_hosforaldrar";
                case EtuaHousingStatus.CompanyResidence: return "housing_tjanstebostad";
                case EtuaHousingStatus.Other: return "housing_hyresbostad";
                default:
                    throw new NotImplementedException();
            }
        }

        protected override Tuple<bool, PreCreditClient.CreditApplicationRequest> DoTranslate()
        {
            const string app = "application";

            Action<string, string, bool> parseApplicant = (externalPrefix, internalPrefix, isCoApplicant) =>
            {
                OptionalBool($"{externalPrefix}approvedSat", internalPrefix, "approvedSat");
                RequiredCivicRegNumberFi($"{externalPrefix}civicRegNr", internalPrefix, "civicRegNr", _ => internalItems.Add(new PreCreditClient.CreditApplicationRequest.Item { Group = internalPrefix, Name = "civicRegNrCountry", Value = "FI" }));
                RequiredString($"{externalPrefix}phone", internalPrefix, "phone", 100);
                RequiredEmail($"{externalPrefix}email", internalPrefix, "email");
                RequiredEnum<EtuaMaritalStatus>($"{externalPrefix}maritalStatus", internalPrefix, "marriage", MapEtuaMaritalStatus);
                RequiredInt($"{ externalPrefix}nrOfChildren", internalPrefix, "nrOfChildren");
                RequiredEnum<EtuaEmploymentStatus>($"{externalPrefix}employment", internalPrefix, "employment", MapEtuaEmploymentStatus);
                RequiredEnum<EtuaEducationStatus>($"{externalPrefix}education", internalPrefix, "education", MapEtuaEducationStatus);
                RequiredInt($"{externalPrefix}incomePerMonthAmount", internalPrefix, "incomePerMonthAmount");
                RequiredEnum<EtuaHousingStatus>($"{ externalPrefix}housing", internalPrefix, "housing", MapEtuaHousingStatus);
                OptionalString($"{externalPrefix}employer", internalPrefix, "employer", 150);
                OptionalString($"{externalPrefix}employerPhone", internalPrefix, "employerPhone", 100);
                OptionalMonth($"{externalPrefix}employedSinceMonth", internalPrefix, "employedSinceMonth");
                OptionalInt($"{externalPrefix}housingCostPerMonthAmount", internalPrefix, "housingCostPerMonthAmount"); 
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

            var nrOfApplicants = 1;
            //Applicant 1
            parseApplicant("", "applicant1", false);

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
                ProviderName = NEnv.EtuaProviderName,
                UserLanguage = "fi",
                Items = internalItems.ToArray()
            });
        }
    }
}