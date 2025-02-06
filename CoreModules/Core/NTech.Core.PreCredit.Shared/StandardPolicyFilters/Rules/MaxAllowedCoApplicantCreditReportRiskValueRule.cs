namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class MaxAllowedCoApplicantCreditReportRiskValueRule : Rule
    {
        public override string Name => "MaxAllowedCoApplicantCreditReportRiskValue";

        public override bool IsEvaluatedPerApplicant => false;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables("coApplicantCreditReportRiskValue");

        public override string[] RequestedApplicantLevelVaribles => CreateVariables();

        public override StaticRuleParameter[] StaticParameters => CreateParameters(CreateDecimalStaticParameter("maxCreditReportRiskValue"));

        public override string GetDescription(string country, string language) =>
            "v:coApplicantCreditReportRiskValue > s:maxCreditReportRiskValue";

        public override string GetDisplayName(string country, string language) => "Max allowed co applicant credit report riskvalue";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context)
        {
            if (context.Variables.GetString("coApplicantCreditReportRiskValue", true) == "noCoApp")
                return false;
            return context.Variables.GetDecimalRequired("coApplicantCreditReportRiskValue") > context.StaticParameters.GetDecimal("maxCreditReportRiskValue");
        }

        public override string DefaultRejectionReasonName => "score";
    }
}