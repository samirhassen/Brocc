namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class ApplicantMissingCreditReportConsentRule : Rule
    {
        public override string Name => "ApplicantMissingCreditReportConsent";

        public override bool IsEvaluatedPerApplicant => true;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables();

        public override string[] RequestedApplicantLevelVaribles => CreateVariables("isApplicantMissingCreditReportConsent");

        public override StaticRuleParameter[] StaticParameters => CreateParameters();

        public override string GetDescription(string country, string language) => "v:isApplicantMissingCreditReportConsent = Yes";

        public override string GetDisplayName(string country, string language) => "Applicant missing credit report consent";

        public override string GetVariableDisplay(string country, string uiLanguage, ScopedVariableSet variables) =>
            GetBooleanDisplayValue("isApplicantMissingCreditReportConsent", uiLanguage, variables);

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetBoolRequired("isApplicantMissingCreditReportConsent");
    }
}