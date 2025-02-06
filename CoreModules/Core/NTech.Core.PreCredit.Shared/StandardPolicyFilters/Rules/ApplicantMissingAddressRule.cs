namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class ApplicantMissingAddressRule : Rule
    {
        public override string Name => "ApplicantMissingAddress";

        public override bool IsEvaluatedPerApplicant => true;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables();

        public override string[] RequestedApplicantLevelVaribles => CreateVariables("applicantHasAddress");

        public override StaticRuleParameter[] StaticParameters => CreateParameters();

        public override string GetDescription(string country, string language) => "v:applicantHasAddress = false";

        public override string GetDisplayName(string country, string language) => "Applicant missing address";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) => !context.Variables.GetBoolRequired("applicantHasAddress");

        public override string DefaultRejectionReasonName => "address";
    }
}