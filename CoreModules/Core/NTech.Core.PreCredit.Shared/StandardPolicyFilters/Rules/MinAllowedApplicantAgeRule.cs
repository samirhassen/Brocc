namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class MinAllowedApplicantAgeRule : Rule
    {
        public override string Name => "MinAllowedApplicantAge";

        public override bool IsEvaluatedPerApplicant => true;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables();

        public override string[] RequestedApplicantLevelVaribles => CreateVariables("applicantAgeInYears");

        public override StaticRuleParameter[] StaticParameters => CreateParameters(CreateIntStaticParameter("minApplicantAgeInYears"));

        public override string GetDescription(string country, string language) => "v:applicantAgeInYears < s:minApplicantAgeInYears";

        public override string GetDisplayName(string country, string language) => "Min allowed applicant age";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetIntRequired("applicantAgeInYears") < context.StaticParameters.GetInt("minApplicantAgeInYears");

    }
}