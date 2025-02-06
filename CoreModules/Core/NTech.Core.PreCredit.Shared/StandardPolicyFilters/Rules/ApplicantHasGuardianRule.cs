namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class ApplicantHasGuardianRule : Rule
    {
        public override string Name => "ApplicantHasGuardian";

        public override bool IsEvaluatedPerApplicant => true;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables();

        public override string[] RequestedApplicantLevelVaribles => CreateVariables("applicantHasGuardian");

        public override StaticRuleParameter[] StaticParameters => CreateParameters();

        public override string GetDescription(string country, string language) => "v:applicantHasGuardian = true";

        public override string GetDisplayName(string country, string language) => "Applicant has guardian";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetBoolRequired("applicantHasGuardian");
    }
}