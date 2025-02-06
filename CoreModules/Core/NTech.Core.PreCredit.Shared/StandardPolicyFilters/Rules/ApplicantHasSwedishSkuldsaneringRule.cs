namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class ApplicantHasSwedishSkuldsaneringRule : Rule
    {
        public override string Name => "ApplicantHasSwedishSkuldsanering";

        public override bool IsEvaluatedPerApplicant => true;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables();

        public override string[] RequestedApplicantLevelVaribles => CreateVariables("applicantCreditReportHasSESkuldsanering");

        public override StaticRuleParameter[] StaticParameters => CreateParameters();

        public override string GetDescription(string country, string language) => "v:applicantCreditReportHasSESkuldsanering = true";

        public override string GetDisplayName(string country, string language) => "Applicant has swedish skuldsanering";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetBoolRequired("applicantCreditReportHasSESkuldsanering");
    }
}