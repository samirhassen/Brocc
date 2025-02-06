namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class MaxAllowedMainApplicantPaymentRemarksRule : Rule
    {
        public override string Name => "MaxAllowedMainApplicantPaymentRemarks";

        public override bool IsEvaluatedPerApplicant => false;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables("mainApplicantCreditReportNrOfPaymentRemarks");

        public override string[] RequestedApplicantLevelVaribles => CreateVariables();

        public override StaticRuleParameter[] StaticParameters => CreateParameters(CreateIntStaticParameter("maxNrOfPaymentRemarks"));

        public override string GetDescription(string country, string language) =>
            "v:mainApplicantCreditReportNrOfPaymentRemarks > s:maxNrOfPaymentRemarks";

        public override string GetDisplayName(string country, string language) => "Max allowed main applicant payment remarks";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetIntRequired("mainApplicantCreditReportNrOfPaymentRemarks") > context.StaticParameters.GetInt("maxNrOfPaymentRemarks");

        public override string DefaultRejectionReasonName => "paymentRemark";
    }
}