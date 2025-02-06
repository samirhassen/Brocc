namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class ApplicantClaimsToBeGuarantorRule : Rule
    {
        public override string Name => "ApplicantClaimsToBeGuarantor";

        public override bool IsEvaluatedPerApplicant => true;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables();

        public override string[] RequestedApplicantLevelVaribles => CreateVariables("applicantClaimsToBeGuarantor");

        public override StaticRuleParameter[] StaticParameters => CreateParameters();

        public override string GetDescription(string country, string language) => "v:applicantClaimsToBeGuarantor = true";

        public override string GetDisplayName(string country, string language) => "Applicant claims to be guarantor";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetBoolRequired("applicantClaimsToBeGuarantor");
    }
}