using nPreCredit.Code.Services;
using System;
using System.Linq;

namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class MinAllowedWeightedAverageSettlementInterestRateRule : Rule
    {
        public override string Name => "MinAllowedWeightedAverageSettlementInterestRate";

        public override bool IsEvaluatedPerApplicant => false;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables("weightedAverageSettlementInterestRatePercent", "hasLoansToSettle");

        public override string[] RequestedApplicantLevelVaribles => CreateVariables();

        public override StaticRuleParameter[] StaticParameters =>
            CreateParameters(CreatePercentStaticParameter("minInterestRatePercent"));

        public override string GetDescription(string country, string language) =>
            "v:hasLoansToSettle and v:weightedAverageSettlementInterestRatePercent < s:minInterestRatePercent";

        public override string GetDisplayName(string country, string language) => "Min weighted average settlement interest rate";

        public override string GetVariableDisplay(string country, string uiLanguage, ScopedVariableSet variables) =>
            StaticRuleParameter.FormatPercentForDisplay(variables.GetDecimalOptional("weightedAverageSettlementInterestRatePercent"), country, uiLanguage);

        protected override bool? IsRejectedByRule(EvaluateRuleContext context)
        {
            var hasLoansToSettle = context.Variables.GetBoolRequired("hasLoansToSettle");
            if (!hasLoansToSettle)
                return false;

            return context.Variables.GetDecimalRequired("weightedAverageSettlementInterestRatePercent") < context.StaticParameters.GetDecimal("minInterestRatePercent");
        }

        public override string DefaultRejectionReasonName => "requestedOffer";

        public static (decimal? WeightedAverageSettlementInterestRatePercent, decimal? MinSettlementInterestRatePercent, bool HasLoansToSettle) ComputeWeightedSettlementInterestRateScoringVariables(ComplexApplicationList loansToSettle)
        {
            var allLoansToSettle = loansToSettle
                .GetRows()
                .Where(row => row.GetUniqueItemBoolean("shouldBeSettled") == true)
                .Select(x => new
                {
                    currentDebtAmount = x.GetUniqueItemDecimal("currentDebtAmount") ?? 0m,
                    currentInterestRatePercent = x.GetUniqueItemDecimal("currentInterestRatePercent") ?? 0m
                })
                .ToList();

            if (allLoansToSettle.Count == 0m)
                return (WeightedAverageSettlementInterestRatePercent: null, MinSettlementInterestRatePercent: null, HasLoansToSettle: false);

            var totalLoanAmountToSettle = allLoansToSettle.Aggregate(0m, (x, y) => x + y.currentDebtAmount);

            if (totalLoanAmountToSettle == 0m)
                return (WeightedAverageSettlementInterestRatePercent: null, MinSettlementInterestRatePercent: null, HasLoansToSettle: false);

            var minSettlementInterestRatePercent = allLoansToSettle.Min(x => x.currentInterestRatePercent);
            var weightedAverageSettlementInterestRatePercent = Math.Round(allLoansToSettle.Sum(x => x.currentDebtAmount * x.currentInterestRatePercent) / totalLoanAmountToSettle, 2);

            return (
                WeightedAverageSettlementInterestRatePercent: weightedAverageSettlementInterestRatePercent,
                MinSettlementInterestRatePercent: minSettlementInterestRatePercent,
                HasLoansToSettle: true
            );
        }
    }
}
