namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class ApplicantHasActiveCheckpointRule : Rule
    {
        public override string Name => "ApplicantHasActiveCheckpoint";

        public override bool IsEvaluatedPerApplicant => true;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables();

        public override string[] RequestedApplicantLevelVaribles => CreateVariables("applicantHasActiveCheckpoint");

        public override StaticRuleParameter[] StaticParameters => CreateParameters();

        public override string GetDescription(string country, string language) => "v:applicantHasActiveCheckpoint = true";

        public override string GetDisplayName(string country, string language) => "Applicant has active checkpoint";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetBoolRequired("applicantHasActiveCheckpoint");
    }
}