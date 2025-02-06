namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class MaxAllowedLtiRule : Rule
    {
        public override string Name => "MaxAllowedLti";

        public override bool IsEvaluatedPerApplicant => false;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables("loanToIncome");

        public override string[] RequestedApplicantLevelVaribles => CreateVariables();

        public override StaticRuleParameter[] StaticParameters => CreateParameters(CreateDecimalStaticParameter("maxAllowedLti"));

        public override string GetDescription(string country, string language) => "v:loanToIncome < s:maxAllowedLti";

        public override string GetDisplayName(string country, string language) => "Max allowed lti";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetDecimalRequired("loanToIncome") > context.StaticParameters.GetDecimal("maxAllowedLti");

        public override string DefaultRejectionReasonName => "score";
    }
}