using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace nGccCustomerApplication.Code.ProviderIntegrations
{
    public class EoneIntegration : ProviderIntegrationBase
    {
        private enum EoneMaritalStatus
        {
            Married,
            Cohabitation,
            Single,
            Divorced,
            Widower
        }
        private static string MapEoneMaritalStatus(EoneMaritalStatus s)
        {
            switch (s)
            {
                case EoneMaritalStatus.Cohabitation: return "marriage_sambo";
                case EoneMaritalStatus.Divorced: return "marriage_ogift";
                case EoneMaritalStatus.Married: return "marriage_gift";
                case EoneMaritalStatus.Single: return "marriage_ogift";
                case EoneMaritalStatus.Widower: return "marriage_ogift";
                default:
                    throw new NotImplementedException();
            }
        }

        private enum EoneEmploymentStatus
        {
            Permanent,
            FixedTerm,
            PartTime,
            Entrepreneur,
            Pensioner,
            Unemployed,
            Student
        }
        private static string MapEoneEmploymentStatus(EoneEmploymentStatus s)
        {
            switch (s)
            {
                case EoneEmploymentStatus.Permanent: return "employment_fastanstalld";
                case EoneEmploymentStatus.FixedTerm: return "employment_visstidsanstalld";
                case EoneEmploymentStatus.PartTime: return "employment_visstidsanstalld";
                case EoneEmploymentStatus.Entrepreneur: return "employment_foretagare";
                case EoneEmploymentStatus.Pensioner: return "employment_pensionar";
                case EoneEmploymentStatus.Unemployed: return "employment_arbetslos";
                case EoneEmploymentStatus.Student: return "employment_studerande";
                default:
                    throw new NotImplementedException();
            }
        }

        private enum EoneEducationStatus
        {
            PrimarySchool,
            Gymnasium,
            VocationalSchool,
            BachelorsDegree,
            GraduateSchool
        }
        private static string MapEoneEducationStatus(EoneEducationStatus s)
        {
            switch (s)
            {
                case EoneEducationStatus.PrimarySchool: return "education_grundskola";
                case EoneEducationStatus.Gymnasium: return "education_gymnasie";
                case EoneEducationStatus.VocationalSchool: return "education_yrkesskola";
                case EoneEducationStatus.BachelorsDegree: return "education_hogskola";
                case EoneEducationStatus.GraduateSchool: return "education_hogskola";
                default:
                    throw new NotImplementedException();
            }
        }

        private enum EoneHousingStatus
        {
            Oma,
            Asumisoikeus,
            Vuokra,
            VanhempienLuona
        }
        private static string MapEoneHousingStatus(EoneHousingStatus s)
        {
            switch (s)
            {
                case EoneHousingStatus.Oma: return "housing_egenbostad";
                case EoneHousingStatus.Asumisoikeus: return "housing_bostadsratt";
                case EoneHousingStatus.Vuokra: return "housing_hyresbostad";
                case EoneHousingStatus.VanhempienLuona: return "housing_hosforaldrar";
                default:
                    throw new NotImplementedException();
            }
        }
        
        protected override Tuple<bool, PreCreditClient.CreditApplicationRequest> DoTranslate()
        {
            const string app = "application";

            Action<string, string, bool> parseApplicant = (externalPrefix, internalPrefix, isCoApplicant) =>
            {
                OptionalBool($"{externalPrefix}tarkistus", internalPrefix, "approvedSat");
                RequiredCivicRegNumberFi($"{externalPrefix}sotu", internalPrefix, "civicRegNr", _ => internalItems.Add(new PreCreditClient.CreditApplicationRequest.Item { Group = internalPrefix, Name = "civicRegNrCountry", Value = "FI" }));
                RequiredString($"{externalPrefix}puhelin", internalPrefix, "phone", 100);
                RequiredEmail($"{externalPrefix}email", internalPrefix, "email");
                RequiredEnum<EoneMaritalStatus>($"{externalPrefix}maritalStatus", internalPrefix, "marriage", MapEoneMaritalStatus);
                RequiredInt("lapset", internalPrefix, "nrOfChildren"); //intentionally copied from main applicant to co applicant since eone doesnt ask this
                RequiredEnum<EoneEmploymentStatus>($"{externalPrefix}tyosuhde", internalPrefix, "employment", MapEoneEmploymentStatus);
                RequiredEnum<EoneEducationStatus>($"{externalPrefix}koulutus", internalPrefix, "education", MapEoneEducationStatus);
                RequiredInt($"{externalPrefix}bruttotulot", internalPrefix, "incomePerMonthAmount", transformBeforeAdd: x => (int)Math.Round(((decimal)x) / 12m));
                RequiredEnum<EoneHousingStatus>("asumismuoto", internalPrefix, "housing", MapEoneHousingStatus);  //intentionally copied from main applicant to co applicant since eone doesnt ask this
                OptionalString($"{externalPrefix}tyonantaja", internalPrefix, "employer", 150);
                OptionalString($"{externalPrefix}tyonantajanpuh", internalPrefix, "employerPhone", 100);
                OptionalMonth($"{externalPrefix}tyonalkupvm", internalPrefix, "employedSinceMonth");
                OptionalInt("vuokra", internalPrefix, "housingCostPerMonthAmount"); //intentionally copied from main applicant to co applicant since eone doesnt ask this
                OptionalInt($"{externalPrefix}korttilaina", internalPrefix, "creditCardAmount");
                OptionalInt($"{externalPrefix}lklainakulut", internalPrefix, "creditCardCostPerMonthAmount");
                OptionalInt($"{externalPrefix}asuntolaina", internalPrefix, "mortgageLoanAmount");
                OptionalInt($"{externalPrefix}aslainakulut", internalPrefix, "mortgageLoanCostPerMonthAmount");
                OptionalInt($"{externalPrefix}opintolaina", internalPrefix, "studentLoanAmount");
                OptionalInt($"{externalPrefix}oplainakulut", internalPrefix, "studentLoanCostPerMonthAmount");
                OptionalInt($"{externalPrefix}autolaina", internalPrefix, "carOrBoatLoanAmount");
                OptionalInt($"{externalPrefix}atlainakulut", internalPrefix, "carOrBoatLoanCostPerMonthAmount");
                OptionalInt($"{externalPrefix}muutlaina", internalPrefix, "otherLoanAmount");
                OptionalInt($"{externalPrefix}mulainakulut", internalPrefix, "otherLoanCostPerMonthAmount");
            };

            //Application
            RequiredDecimal("luottoraja", app, "amount");
            RequiredInt("maksuaika", app, "repaymentTimeInYears");
            OptionalInt("poismaksu", app, "loansToSettleAmount");

            var nrOfApplicants = 1;
            //Applicant 1
            parseApplicant("", "applicant1", false);

            //Applicant 2            
            const string coApplicantExternalPrefix = "a2.";
            if(externalItems.Any(x => x.Name.StartsWith(coApplicantExternalPrefix)))
            {
                nrOfApplicants = 2;
                parseApplicant(coApplicantExternalPrefix, "applicant2", true);
            }

            if (nrOfApplicants == 2 
                && internalItems.Any(x => x.Group == "applicant1" && x.Name == "approvedSat") 
                && !internalItems.Any(x => x.Group == "applicant2" && x.Name == "approvedSat"))
            {
                //Eone asks both customers at the same time so yes means yes for both.
                internalItems.Add(new PreCreditClient.CreditApplicationRequest.Item
                {
                    Group = "applicant2",
                    Name = "approvedSat",
                    Value = internalItems.Single(x => x.Group == "applicant1" && x.Name == "approvedSat").Value
                });
            }

            return Tuple.Create(true, new PreCreditClient.CreditApplicationRequest
            {
                NrOfApplicants = nrOfApplicants,
                ProviderName = NEnv.EoneProviderName,
                UserLanguage = "fi",
                Items = internalItems.ToArray()
            });
        }
    }
}