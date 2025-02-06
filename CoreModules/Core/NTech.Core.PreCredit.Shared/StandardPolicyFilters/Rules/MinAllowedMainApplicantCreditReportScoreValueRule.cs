namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class MinAllowedMainApplicantCreditReportScoreValueRule : Rule
    {
        public override string Name => "MinAllowedMainApplicantCreditReportScoreValue";

        public override bool IsEvaluatedPerApplicant => false;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables("mainApplicantCreditReportScoreValue");

        public override string[] RequestedApplicantLevelVaribles => CreateVariables();

        public override StaticRuleParameter[] StaticParameters => CreateParameters(CreateDecimalStaticParameter("minCreditReportScoreValue"));

        public override string GetDescription(string country, string language) =>
            "v:mainApplicantCreditReportScoreValue < s:minCreditReportScoreValue";

        public override string GetDisplayName(string country, string language) => "Min allowed main applicant credit report score";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetDecimalRequired("mainApplicantCreditReportScoreValue") < context.StaticParameters.GetDecimal("minCreditReportScoreValue");
        
        public override string DefaultRejectionReasonName => "score";
    }
}