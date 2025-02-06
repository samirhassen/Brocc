namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class MaxAllowedDbrRule : Rule
    {
        public override string Name => "MaxAllowedDbr";

        public override bool IsEvaluatedPerApplicant => false;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables("debtBurdenRatio");

        public override string[] RequestedApplicantLevelVaribles => CreateVariables();

        public override StaticRuleParameter[] StaticParameters => CreateParameters(CreateDecimalStaticParameter("maxAllowedDbr"));

        public override string GetDescription(string country, string language) => "v:debtBurdenRatio < s:maxAllowedDbr";

        public override string GetDisplayName(string country, string language) => "Max allowed dbr";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetDecimalRequired("debtBurdenRatio") > context.StaticParameters.GetDecimal("maxAllowedDbr");

        public override string DefaultRejectionReasonName => "score";
    }
}