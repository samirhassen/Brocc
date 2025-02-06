namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class MaxAllowedApplicantIncomeRule : Rule
    {
        public override string Name => "MaxAllowedApplicantIncome";

        public override bool IsEvaluatedPerApplicant => true;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables();

        public override string[] RequestedApplicantLevelVaribles => CreateVariables("applicantIncomePerMonth");

        public override StaticRuleParameter[] StaticParameters => CreateParameters(CreateIntStaticParameter("maxApplicantIncomePerMonth"));

        public override string GetDescription(string country, string language) => "v:applicantIncomePerMonth > s:maxApplicantIncomePerMonth";

        public override string GetDisplayName(string country, string language) => "Max allowed applicant income";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetIntRequired("applicantIncomePerMonth") > context.StaticParameters.GetInt("maxApplicantIncomePerMonth");
    }
}