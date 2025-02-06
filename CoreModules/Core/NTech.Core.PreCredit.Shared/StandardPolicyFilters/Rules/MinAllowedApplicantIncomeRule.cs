namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class MinAllowedApplicantIncomeRule : Rule
    {
        public override string Name => "MinAllowedApplicantIncome";

        public override bool IsEvaluatedPerApplicant => true;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables();

        public override string[] RequestedApplicantLevelVaribles => CreateVariables("applicantIncomePerMonth");

        public override StaticRuleParameter[] StaticParameters => CreateParameters(CreateIntStaticParameter("minApplicantIncomePerMonth"));

        public override string GetDescription(string country, string language) => "v:applicantIncomePerMonth < s:minApplicantIncomePerMonth";

        public override string GetDisplayName(string country, string language) => "Min allowed applicant income";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetIntRequired("applicantIncomePerMonth") < context.StaticParameters.GetInt("minApplicantIncomePerMonth");
    }
}