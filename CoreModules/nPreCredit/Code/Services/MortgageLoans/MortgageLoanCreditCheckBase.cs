using NTech;
using NTech.Banking.ScoringEngine;
using NTech.Legacy.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public abstract class MortgageLoanCreditCheckBase
    {
        public MortgageLoanCreditCheckBase(IClock clock, IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository)
        {
            this.clock = clock;
            this.partialCreditApplicationModelRepository = partialCreditApplicationModelRepository;
        }

        protected readonly IClock clock;
        private readonly IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository;

        protected void FillScoringInputFromApplicationModel(string applicationNr, ScoringDataModel input,
            ISet<string> additionalApplicationFields = null,
            ISet<string> additionalApplicantFields = null,
            Action<PartialCreditApplicationModel> withAppModel = null)
        {
            additionalApplicationFields = additionalApplicationFields ?? new HashSet<string>();
            additionalApplicantFields = additionalApplicantFields ?? new HashSet<string>();

            var civicRegNrParser = NEnv.BaseCivicRegNumberParser;
            var applicationFields = new HashSet<string>() { "mortgageLoanHouseZipcode", "mortgageLoanHouseMonthlyFeeAmount" };
            var applicantsFields = new HashSet<string>() { "civicRegNr", "customerId", "employedSinceMonth", "mortgageLoanOtherPropertyTypes", "incomePerMonthAmount", "employment2", "marriage2", "nrOfChildren", "savingsAmount" };

            var applicationLoanTypes = new string[] { "carOrBoatLoan", "creditCard", "studentLoan", "mortgageLoan", "otherLoan" };
            foreach (var applicationLoanType in applicationLoanTypes)
            {
                additionalApplicantFields.Add($"{applicationLoanType}Amount");
                additionalApplicantFields.Add($"{applicationLoanType}CostPerMonthAmount");
            }

            if (additionalApplicationFields != null)
                applicationFields.UnionWith(additionalApplicationFields);
            if (additionalApplicantFields != null)
                applicantsFields.UnionWith(additionalApplicantFields);

            var applicationModel = partialCreditApplicationModelRepository.Get(applicationNr, new PartialCreditApplicationModelRequest
            {
                ErrorIfGetNonLoadedField = true,
                ApplicationFields = applicationFields.ToList(),
                ApplicantFields = applicantsFields.ToList()
            });

            input.Set("nrOfApplicants", applicationModel.NrOfApplicants, null);

            input.Set("objectMonthlyFee", applicationModel.Application.Get("mortgageLoanHouseMonthlyFeeAmount").DecimalValue.Optional, null);
            input.Set("objectZipcode", applicationModel.Application.Get("mortgageLoanHouseZipcode").StringValue.Optional, null);

            var totalCombinedOtherLoansAmount = 0m;
            var totalCombinedOtherMortgageLoansAmount = 0m;
            var combinedNrOfChildren = 0;
            bool anyHasNrOfChildren = false;
            applicationModel.DoForEachApplicant(applicantNr =>
            {
                var employedSinceMonth = applicationModel.Applicant(applicantNr).Get("employedSinceMonth").MonthValue(false).Optional;
                if (employedSinceMonth.HasValue)
                    input.Set("employmentMonths", Dates.GetAbsoluteNrOfMonthsBetweenDates(employedSinceMonth.Value, clock.Today), applicantNr: applicantNr);
                else
                    input.Set("employmentMonths", 0, applicantNr: applicantNr);

                var civicRegNr = civicRegNrParser.Parse(applicationModel.Applicant(applicantNr).Get("civicRegNr").StringValue.Required);
                var ageInYears = CivicRegNumbers.ComputeAgeFromCivicRegNumber(civicRegNr, CoreClock.SharedInstance);
                if (ageInYears.HasValue)
                    input.Set("ageInYears", ageInYears.Value, applicantNr: applicantNr);
                input.Set("otherPropertyTypes", applicationModel.Applicant(applicantNr).Get("mortgageLoanOtherPropertyTypes").StringValue.Optional, applicantNr: applicantNr);

                input.Set("otherLoansMonthlyCost", applicationLoanTypes.Sum(loanType => applicationModel.Applicant(applicantNr).Get($"{loanType}CostPerMonthAmount").DecimalValue.Required), applicantNr: applicantNr);
                totalCombinedOtherLoansAmount += applicationLoanTypes.Sum(loanType => applicationModel.Applicant(applicantNr).Get($"{loanType}Amount").DecimalValue.Required);
                totalCombinedOtherMortgageLoansAmount += applicationModel.Applicant(applicantNr).Get($"mortgageLoanAmount").DecimalValue.Required;
                input.Set("carOrBoatLoanAmount", applicationModel.Applicant(applicantNr).Get($"carOrBoatLoanAmount").DecimalValue.Required, applicantNr: applicantNr);
                input.Set("creditCardAmount", applicationModel.Applicant(applicantNr).Get($"creditCardAmount").DecimalValue.Required, applicantNr: applicantNr);
                input.Set("studentLoanAmount", applicationModel.Applicant(applicantNr).Get($"studentLoanAmount").DecimalValue.Required, applicantNr: applicantNr);
                input.Set("otherLoanAmount", applicationModel.Applicant(applicantNr).Get($"otherLoanAmount").DecimalValue.Required, applicantNr: applicantNr);

                input.Set("savingsAmount", applicationModel.Applicant(applicantNr).Get("savingsAmount").DecimalValue.Required, applicantNr: applicantNr);
                input.Set("grossMonthlyIncome", applicationModel.Applicant(applicantNr).Get("incomePerMonthAmount").DecimalValue.Optional, applicantNr: applicantNr);
                input.Set("employment", applicationModel.Applicant(applicantNr).Get("employment2").StringValue.Optional, applicantNr: applicantNr);
                input.Set("marriage", applicationModel.Applicant(applicantNr).Get("marriage2").StringValue.Optional, applicantNr: applicantNr);
                var nrOfChildren = applicationModel.Applicant(applicantNr).Get("nrOfChildren").IntValue.Optional;
                if (nrOfChildren.HasValue)
                {
                    anyHasNrOfChildren = true;
                    combinedNrOfChildren += nrOfChildren.Value;
                }
            });
            if (anyHasNrOfChildren)
                input.Set("combinedNrOfChildren", combinedNrOfChildren, null);

            input.Set("totalCombinedOtherLoansAmount", totalCombinedOtherLoansAmount, null);
            input.Set("totalCombinedOtherMortgageLoansAmount", totalCombinedOtherMortgageLoansAmount, null);

            withAppModel?.Invoke(applicationModel);
        }

        protected void FillScoringInputFromCreditHistory(ScoringDataModel input, Dictionary<int, int> customerIdByApplicantNr, ICreditClient creditClient)
        {
            var historyItems = creditClient.GetCustomerCreditHistory(customerIdByApplicantNr.Values.ToList());
            var historyItemsByApplicantNr = customerIdByApplicantNr
                .Keys
                .Select(x => new { applicantNr = x, items = historyItems.Where(y => y.CustomerIds.Contains(customerIdByApplicantNr[x])).ToList() })
                .ToDictionary(x => x.applicantNr, x => x.items);
            foreach (var applicantNr in historyItemsByApplicantNr.Keys)
            {
                //NOTE: Both applicants could share a credit which will then be in both lists so if summing on application level use historyItems instead
                var applicantHistoryItems = historyItemsByApplicantNr[applicantNr];
                input.Set("maxNrOfDaysBetweenDueDateAndPaymentEver", applicantHistoryItems.Max(x => x.MaxNrOfDaysBetweenDueDateAndPaymentEver) ?? 0, applicantNr: applicantNr);
            }
        }
    }
}