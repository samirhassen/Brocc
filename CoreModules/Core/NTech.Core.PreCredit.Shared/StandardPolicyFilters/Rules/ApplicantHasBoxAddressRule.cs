namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class ApplicantHasBoxAddressRule : Rule
    {
        public override string Name => "ApplicantHasBoxAddress";

        public override bool IsEvaluatedPerApplicant => true;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables();

        public override string[] RequestedApplicantLevelVaribles => CreateVariables("applicantHasBoxAddress");

        public override StaticRuleParameter[] StaticParameters => CreateParameters();

        public override string GetDescription(string country, string language) =>
            "v:applicantHasBoxAddress = true";

        public override string GetDisplayName(string country, string language) =>
            "Applicant has box address";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetBoolRequired("applicantHasBoxAddress");

        public override string DefaultRejectionReasonName => "address";
    }
}