namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class MaxAllowedMainApplicantCreditReportRiskValueRule : Rule
    {
        public override string Name => "MaxAllowedMainApplicantCreditReportRiskValue";

        public override bool IsEvaluatedPerApplicant => false;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables("mainApplicantCreditReportRiskValue");

        public override string[] RequestedApplicantLevelVaribles => CreateVariables();

        public override StaticRuleParameter[] StaticParameters => CreateParameters(CreateDecimalStaticParameter("maxCreditReportRiskValue"));

        public override string GetDescription(string country, string language) =>
            "v:mainApplicantCreditReportRiskValue > s:maxCreditReportRiskValue";

        public override string GetDisplayName(string country, string language) => "Max allowed main applicant credit report riskvalue";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetDecimalRequired("mainApplicantCreditReportRiskValue") > context.StaticParameters.GetDecimal("maxCreditReportRiskValue");

        public override string DefaultRejectionReasonName => "score";
    }
}