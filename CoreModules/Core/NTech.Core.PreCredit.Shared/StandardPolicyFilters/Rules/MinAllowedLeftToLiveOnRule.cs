namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class MinAllowedLeftToLiveOnRule : Rule
    {
        public override string Name => "MinAllowedLeftToLiveOn";

        public override bool IsEvaluatedPerApplicant => false;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables("leftToLiveOnAmount");

        public override string[] RequestedApplicantLevelVaribles => CreateVariables();

        public override StaticRuleParameter[] StaticParameters =>
            CreateParameters(CreateIntStaticParameter("minLeftToLiveOnAmount"));

        public override string GetDescription(string country, string language) =>
            "v:leftToLiveOnAmount < s:minLeftToLiveOnAmount";

        public override string GetDisplayName(string country, string language) => "Min allowed ltl";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetIntRequired("leftToLiveOnAmount") < context.StaticParameters.GetInt("minLeftToLiveOnAmount");

        public override string DefaultRejectionReasonName => "score";
    }
}