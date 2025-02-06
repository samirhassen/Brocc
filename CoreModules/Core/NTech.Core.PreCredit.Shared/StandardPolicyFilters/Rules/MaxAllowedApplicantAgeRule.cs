namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class MaxAllowedApplicantAgeRule : Rule
    {
        public override string Name => "MaxAllowedApplicantAge";

        public override bool IsEvaluatedPerApplicant => true;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables();

        public override string[] RequestedApplicantLevelVaribles => CreateVariables("applicantAgeInYears");

        public override StaticRuleParameter[] StaticParameters => CreateParameters(CreateIntStaticParameter("maxApplicantAgeInYears"));

        public override string GetDescription(string country, string language) => "v:applicantAgeInYears > s:maxApplicantAgeInYears";

        public override string GetDisplayName(string country, string language) => "Max allowed applicant age";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetIntRequired("applicantAgeInYears") > context.StaticParameters.GetInt("maxApplicantAgeInYears");
    }
}