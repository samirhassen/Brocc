namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class MinAllowedDataShareApplicantIncomeRule : Rule
    {
        public override string Name => "MinAllowedDataShareApplicantIncome";

        public override bool IsEvaluatedPerApplicant => true;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables();

        public override string[] RequestedApplicantLevelVaribles => CreateVariables("applicantDataShareIncomePerMonth");

        public override StaticRuleParameter[] StaticParameters => CreateParameters(CreateIntStaticParameter("minApplicantDataShareIncomePerMonth"));

        public override string GetDescription(string country, string language) => "v:applicantDataShareIncomePerMonth < s:minApplicantDataShareIncomePerMonth";

        public override string GetDisplayName(string country, string language) => "Min allowed data share applicant income";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetIntRequired("applicantDataShareIncomePerMonth") < context.StaticParameters.GetInt("minApplicantDataShareIncomePerMonth");
    }
}