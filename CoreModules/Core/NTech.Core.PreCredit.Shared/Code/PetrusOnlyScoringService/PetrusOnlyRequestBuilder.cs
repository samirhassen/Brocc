using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace NTech.Core.PreCredit.Shared.Code.PetrusOnlyScoringService
{
    public static partial class PetrusOnlyRequestBuilder
    {
        public static JObject CreateWebserviceRequest(PetrusOnlyCreditCheckRequest request)
        {
            var application = request.DataContext.Application;
            var loanAmount = application.Application.Get("amount").DecimalValue.Required;
            var mainApplicantCivicRegNr = application.Applicant(1).Get("civicRegNr").StringValue.Required;

            var requestApplication = new ExpandoObject().SetValues(x =>
            {
                var a = application.Application;
                x["loanAmount"] = loanAmount;
                x["externalId"] = request.DataContext.ApplicationNr;
                x["nrOfApplicants"] = request.DataContext.Application.NrOfApplicants;
                x["campaignCode"] = a.Get("campaignCode").StringValue.Optional;
                x["loansToSettleAmount"] = a.Get("loansToSettleAmount").DecimalValue.Optional;
                x["providerApplicationId"] = a.Get("providerApplicationId").StringValue.Optional;
                x["repaymentTimeInMonths"] = a.Get("repaymentTimeInYears").IntValue.Required * 12;
                x["bacsChannel"] = request.ProviderName;
                x["referenceInterestRatePercent"] = request.ReferenceInterestRatePercent;
            });

            void PopulateApplicant(int applicantNr, IDictionary<string, object> x)
            {
                var a = application.Applicant(applicantNr);
                CopiedApplicantBooleanFields.ForEach(fieldName => x[fieldName] = a.Get(fieldName).BoolValue.Optional);
                CopiedApplicantDecimalFields.ForEach(fieldName => x[fieldName] = a.Get(fieldName).DecimalValue.Optional);
                CopiedApplicantStringFields.ForEach(fieldName => x[fieldName] = a.Get(fieldName).StringValue.Optional);
                x["nrOfChildren"] = a.Get("nrOfChildren").IntValue.Optional;

                var s = request.DataContext.ScoringData;
                x["currentlyOverdueNrOfDays"] = s.GetInt("currentlyOverdueNrOfDays", applicantNr);
                x["maxNrOfDaysBetweenDueDateAndPaymentEver"] = s.GetInt("maxNrOfDaysBetweenDueDateAndPaymentEver", applicantNr);
                x["historicalDebtCollectionCount"] = s.GetInt("historicalDebtCollectionCount", applicantNr);
                x["maxNrOfDaysBetweenDueDateAndPaymentLastSixMonths"] = s.GetInt("maxNrOfDaysBetweenDueDateAndPaymentLastSixMonths", applicantNr);
                x["nrOfActiveLoans"] = s.GetInt("nrOfActiveLoans", applicantNr);
                x["minNrOfClosedNotificationsOnActiveLoans"] = s.GetInt("minNrOfClosedNotificationsOnActiveLoans", applicantNr);
                x["pausedDays"] = s.GetInt("pausedDays", applicantNr);
                x["activeApplicationCount"] = s.GetInt("activeApplicationCount", applicantNr);
                x["isExistingCustomer"] = FromNullable(s.GetInt("activeApplicationCount", applicantNr), y => y > 0);
                x["maxActiveApplicationAgeInDays"] = s.GetInt("maxActiveApplicationAgeInDays", applicantNr);
                x["existingCustomerBalance"] = s.GetDecimal("existingCustomerBalance", applicantNr);
                x["existingCustomerTotalInterestRatePercent"] = s.GetDecimal("existingCustomerTotalInterestRatePercent", applicantNr);
                x["existingCustomerAnnuity"] = s.GetDecimal("existingCustomerAnnuity", applicantNr);
                x["existingCustomerInitialEffectiveInterestRatePercent"] = s.GetDecimal("existingCustomerInitialEffectiveInterestRatePercent", applicantNr);
                x["isMale"] = s.GetBool("isMale", applicantNr);
                x["ageInYears"] = s.GetInt("ageInYears", applicantNr);
                x["latestApplicationRejectionReason"] = GetMostImportantRejectionReason(s, applicantNr);
                x["latestApplicationRejectionDate"] = s.GetString("latestApplicationRejectionDate", applicantNr);
                x["latestApplicationRejectionAgeInDays"] = s.GetInt("latestApplicationRejectionAgeInDays", applicantNr);
            }

            var petrusRequest = new ExpandoObject().SetValues(x =>
            {
                x["application"] = requestApplication;
                x["mainApplicant"] = new ExpandoObject().SetValues(y => PopulateApplicant(1, y));
                /* Will be added later
                if(application.NrOfApplicants > 1)
                    x["coApplicant"] = new ExpandoObject().SetValues(y => PopulateApplicant(2, y));
                */
            });

            return JObject.FromObject(petrusRequest);
        }

        private static List<string> CopiedApplicantBooleanFields = new List<string>
        {
            "approvedSat", "creditReportConsent", "customerConsent", "informationConsent"
        };

        private static List<string> CopiedApplicantStringFields = new List<string>
        {
            "civicRegNr", "email", "phone", "housing", "marriage", "education", "employedSinceMonth",
            "employer", "employment", "employerPhone"
        };

        private static List<string> CopiedApplicantDecimalFields = new List<string>
        {
            "carOrBoatLoanAmount", "incomePerMonthAmount", "carOrBoatLoanCostPerMonthAmount", "creditCardAmount",
            "creditCardCostPerMonthAmount", "housingCostPerMonthAmount", "mortgageLoanAmount", "mortgageLoanCostPerMonthAmount",
            "otherLoanAmount", "studentLoanAmount", "studentLoanCostPerMonthAmount", "otherLoanCostPerMonthAmount",
        };

        public static IEnumerable<string> RequiredApplicantFields =>
            CopiedApplicantBooleanFields.Concat(CopiedApplicantDecimalFields).Concat(CopiedApplicantStringFields);

        private static TResult? FromNullable<TSource, TResult>(TSource? source, Func<TSource, TResult> f)
            where TSource : struct
            where TResult : struct =>
            source.HasValue ? f(source.Value) : default(TResult);
    }
}
