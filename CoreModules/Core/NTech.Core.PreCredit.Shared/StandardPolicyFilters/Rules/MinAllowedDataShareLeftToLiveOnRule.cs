namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class MinAllowedDataShareLeftToLiveOnRule : Rule
    {
        public override string Name => "MinAllowedDataShareLeftToLiveOn";

        public override bool IsEvaluatedPerApplicant => false;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables("dataShareLeftToLiveOnAmount");

        public override string[] RequestedApplicantLevelVaribles => CreateVariables();

        public override StaticRuleParameter[] StaticParameters =>
            CreateParameters(CreateIntStaticParameter("minDataShareLeftToLiveOnAmount"));

        public override string GetDescription(string country, string language) =>
            "v:dataShareLeftToLiveOnAmount < s:minDataShareLeftToLiveOnAmount";

        public override string GetDisplayName(string country, string language) => "Min allowed data share ltl";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetIntRequired("dataShareLeftToLiveOnAmount") < context.StaticParameters.GetInt("minDataShareLeftToLiveOnAmount");

        public override string DefaultRejectionReasonName => "score";
    }
}