namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class MaxAllowedLtvPercentRule : Rule
    {
        public override string Name => "MaxAllowedLtvPercent";

        public override bool IsEvaluatedPerApplicant => false;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables("loanToValuePercent");

        public override string[] RequestedApplicantLevelVaribles => CreateVariables();

        public override StaticRuleParameter[] StaticParameters => CreateParameters(CreateDecimalStaticParameter("maxAllowedLtvPercent"));

        public override string GetDescription(string country, string language) => "v:loanToValuePercent < s:maxAllowedLtvPercent";

        public override string GetDisplayName(string country, string language) => "Max allowed ltv %";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetDecimalRequired("loanToValuePercent") > context.StaticParameters.GetDecimal("maxAllowedLtvPercent");

        public override string DefaultRejectionReasonName => "score";
    }
}