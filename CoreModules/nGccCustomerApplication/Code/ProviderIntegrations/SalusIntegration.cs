using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace nGccCustomerApplication.Code.ProviderIntegrations
{
    public class SalusIntegration : ProviderIntegrationBase
    {
        private enum SalusMaritalStatus
        {
            married,
            single,
            divorced,
            widow,
            cohabitee,
            unknown
        }

        private static string MapSalusMaritalStatus(SalusMaritalStatus s)
        {
            switch (s)
            {
                case SalusMaritalStatus.married: return "marriage_gift";
                case SalusMaritalStatus.single: return "marriage_ogift";
                case SalusMaritalStatus.divorced: return "marriage_ogift";
                case SalusMaritalStatus.widow: return "marriage_ogift";
                case SalusMaritalStatus.cohabitee: return "marriage_sambo";
                case SalusMaritalStatus.unknown: return "marriage_ogift";
                default:
                    throw new NotImplementedException();
            }
        }
        
        private enum SalusEmploymentStatus
        {
            fulltime,
            entrepreneur,
            freelancer,
            pensioner,
            unemployed,
            student,
            trial,
            parentalleave,
            farmer,
            temp_job,
            other
        }

        private static string MapSalusEmploymentStatus(SalusEmploymentStatus s)
        {
            switch (s)
            {
                case SalusEmploymentStatus.fulltime: return "employment_fastanstalld";
                case SalusEmploymentStatus.entrepreneur: return "employment_foretagare";
                case SalusEmploymentStatus.freelancer: return "employment_visstidsanstalld";
                case SalusEmploymentStatus.pensioner: return "employment_pensionar";
                case SalusEmploymentStatus.unemployed: return "employment_arbetslos";
                case SalusEmploymentStatus.student: return "employment_studerande";
                case SalusEmploymentStatus.trial: return "employment_visstidsanstalld";
                case SalusEmploymentStatus.parentalleave: return "employment_fastanstalld";
                case SalusEmploymentStatus.farmer: return "employment_foretagare";
                case SalusEmploymentStatus.temp_job: return "employment_visstidsanstalld";
                case SalusEmploymentStatus.other: return "employment_arbetslos";
                default:
                    throw new NotImplementedException();
            }
        }

        private enum SalusEducationStatus
        {
            primary_school,
            highschool,
            skilled_work,
            university,
            other
        }
        
        private static string MapSalusEducationStatus(SalusEducationStatus s)
        {
            switch (s)
            {
                case SalusEducationStatus.primary_school: return "education_grundskola";
                case SalusEducationStatus.highschool: return "education_gymnasie";
                case SalusEducationStatus.skilled_work: return "education_yrkesskola";
                case SalusEducationStatus.university: return "education_hogskola";
                case SalusEducationStatus.other: return "education_grundskola";
                default:
                    throw new NotImplementedException();
            }
        }

        private enum SalusHousingStatus
        {
            cohousing,
            employee_housing,
            cohabiting,
            other,
            rental,
            parents,
            condomium,
            residence
        }

        private static string MapSalusHousingStatus(SalusHousingStatus s)
        {
            switch (s)
            {
                case SalusHousingStatus.cohousing: return "housing_hyresbostad";
                case SalusHousingStatus.employee_housing: return "housing_tjanstebostad";
                case SalusHousingStatus.cohabiting: return "housing_hyresbostad";
                case SalusHousingStatus.other: return "housing_hyresbostad";
                case SalusHousingStatus.rental: return "housing_hyresbostad";
                case SalusHousingStatus.parents: return "housing_hosforaldrar";
                case SalusHousingStatus.condomium: return "housing_bostadsratt";
                case SalusHousingStatus.residence: return "housing_egenbostad";
                default:
                    throw new NotImplementedException();
            }
        }

        private enum SalusLoanPurpose
        {
            payout,
            driving_license,
            car,
            wedding,
            home,
            consumption,
            animals,
            doctor,
            domestic_travel,
            cross_border_travel,
            other_house,
            other_capital,
            stocks,
            decoration,
            investment_property,
            plot,
            deposit,
            company,
            tax,
            other
        }

        private static string MapSalusLoanPurpose(SalusLoanPurpose p)
        {
            switch (p)
            {
                case SalusLoanPurpose.payout: return "consumption";
                case SalusLoanPurpose.driving_license: return "consumption";
                case SalusLoanPurpose.car: return "consumption";
                case SalusLoanPurpose.wedding: return "consumption";
                case SalusLoanPurpose.home: return "consumption";
                case SalusLoanPurpose.consumption: return "consumption";
                case SalusLoanPurpose.animals: return "consumption";
                case SalusLoanPurpose.doctor: return "consumption";
                case SalusLoanPurpose.domestic_travel: return "consumption";
                case SalusLoanPurpose.cross_border_travel: return "consumption";
                case SalusLoanPurpose.other_house: return "other";
                case SalusLoanPurpose.other_capital: return "other";
                case SalusLoanPurpose.stocks: return "investment";
                case SalusLoanPurpose.decoration: return "consumption";
                case SalusLoanPurpose.investment_property: return "investment";
                case SalusLoanPurpose.plot: return "other";
                case SalusLoanPurpose.deposit: return "investment";
                case SalusLoanPurpose.company: return "other";
                case SalusLoanPurpose.tax: return "other";
                case SalusLoanPurpose.other: return "other";
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
                RequiredEnum<SalusMaritalStatus>($"{externalPrefix}maritalStatus", internalPrefix, "marriage", MapSalusMaritalStatus);
                RequiredInt($"{ externalPrefix}nrOfChildren", internalPrefix, "nrOfChildren");
                RequiredEnum<SalusEmploymentStatus>($"{externalPrefix}employment", internalPrefix, "employment", MapSalusEmploymentStatus);
                RequiredEnum<SalusEducationStatus>($"{externalPrefix}education", internalPrefix, "education", MapSalusEducationStatus);
                RequiredInt($"{externalPrefix}incomePerMonthAmount", internalPrefix, "incomePerMonthAmount");
                RequiredEnum<SalusHousingStatus>($"{externalPrefix}housing", internalPrefix, "housing", MapSalusHousingStatus);
                OptionalString($"{externalPrefix}employer", internalPrefix, "employer", 150);
                OptionalString($"{externalPrefix}employerPhone", internalPrefix, "employerPhone", 100);
                OptionalMonth($"{externalPrefix}employedSinceMonth", internalPrefix, "employedSinceMonth");
                OptionalInt($"{externalPrefix}housingCostPerMonthAmount", internalPrefix, "housingCostPerMonthAmount");
                AllowedButIgnored($"{externalPrefix}loan_purpose");

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
            OptionalString("campaignCode", app, "campaignCode", 100, filterLinebreaks: true);
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
                ProviderName = NEnv.SalusProviderName,
                UserLanguage = "fi",
                Items = internalItems.ToArray()
            });
        }
    }
}