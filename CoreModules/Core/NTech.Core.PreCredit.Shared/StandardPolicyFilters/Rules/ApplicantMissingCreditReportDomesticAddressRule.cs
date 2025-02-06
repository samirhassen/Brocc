namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class ApplicantMissingCreditReportDomesticAddressRule : Rule
    {
        public override string Name => "ApplicantMissingCreditReportDomesticAddress";

        public override bool IsEvaluatedPerApplicant => true;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables();

        public override string[] RequestedApplicantLevelVaribles => CreateVariables("applicantCreditReportHasDomesticAddress");

        public override StaticRuleParameter[] StaticParameters => CreateParameters();

        public override string GetDescription(string country, string language) => "v:applicantCreditReportHasDomesticAddress = false";

        public override string GetDisplayName(string country, string language) => "Applicant missing credit report domestic address";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) => !context.Variables.GetBoolRequired("applicantCreditReportHasDomesticAddress");

        public override string DefaultRejectionReasonName => "address";
    }
}