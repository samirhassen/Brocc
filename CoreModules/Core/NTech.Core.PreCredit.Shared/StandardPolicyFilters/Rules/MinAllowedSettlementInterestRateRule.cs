namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class MinAllowedSettlementInterestRateRule : Rule
    {
        public override string Name => "MinAllowedSettlementInterestRate";

        public override bool IsEvaluatedPerApplicant => false;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables("minSettlementInterestRatePercent", "hasLoansToSettle");

        public override string[] RequestedApplicantLevelVaribles => CreateVariables();

        public override StaticRuleParameter[] StaticParameters =>
            CreateParameters(CreatePercentStaticParameter("minInterestRatePercent"));

        public override string GetDescription(string country, string language) =>
            "v:hasLoansToSettle and v:minSettlementInterestRatePercent < s:minInterestRatePercent";

        public override string GetDisplayName(string country, string language) => "Min per loan settlement interest rate";

        public override string GetVariableDisplay(string country, string uiLanguage, ScopedVariableSet variables) =>
            StaticRuleParameter.FormatPercentForDisplay(variables.GetDecimalOptional("minSettlementInterestRatePercent"), country, uiLanguage);

        protected override bool? IsRejectedByRule(EvaluateRuleContext context)
        {
            var hasLoansToSettle = context.Variables.GetBoolRequired("hasLoansToSettle");
            if (!hasLoansToSettle)
                return false;

            return context.Variables.GetDecimalRequired("minSettlementInterestRatePercent") < context.StaticParameters.GetDecimal("minInterestRatePercent");
        }

        public override string DefaultRejectionReasonName => "requestedOffer";
    }
}
