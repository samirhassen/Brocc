namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class ApplicantHasKfmBalanceRule : Rule
    {
        public override string Name => "ApplicantHasKfmBalance";

        public override bool IsEvaluatedPerApplicant => true;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables();

        public override string[] RequestedApplicantLevelVaribles => CreateVariables("applicantCreditReportHasKfmBalance");

        public override StaticRuleParameter[] StaticParameters => CreateParameters();

        public override string GetDescription(string country, string language) => "v:applicantCreditReportHasKfmBalance = true";

        public override string GetDisplayName(string country, string language) => "Applicant has kfm balance";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetBoolRequired("applicantCreditReportHasKfmBalance");
    }
}