namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class ApplicantClaimsLegalOrFinancialGuardianRule : Rule
    {
        public override string Name => "ApplicantClaimsLegalOrFinancialGuardian";

        public override bool IsEvaluatedPerApplicant => true;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables();

        public override string[] RequestedApplicantLevelVaribles => CreateVariables("applicantClaimsLegalOrFinancialGuardian");

        public override StaticRuleParameter[] StaticParameters => CreateParameters();

        public override string GetDescription(string country, string language) => "v:applicantClaimsLegalOrFinancialGuardian = true";

        public override string GetDisplayName(string country, string language) => "Applicant claims legal/financial guardian";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetBoolRequired("applicantClaimsLegalOrFinancialGuardian");
    }
}