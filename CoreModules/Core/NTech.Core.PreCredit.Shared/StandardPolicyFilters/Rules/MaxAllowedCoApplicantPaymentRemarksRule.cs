namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class MaxAllowedCoApplicantPaymentRemarksRule : Rule
    {
        public override string Name => "MaxAllowedCoApplicantPaymentRemarks";

        public override bool IsEvaluatedPerApplicant => false;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables("coApplicantCreditReportNrOfPaymentRemarks");

        public override string[] RequestedApplicantLevelVaribles => CreateVariables();

        public override StaticRuleParameter[] StaticParameters => CreateParameters(CreateIntStaticParameter("maxNrOfPaymentRemarks"));

        public override string GetDescription(string country, string language) =>
            "v:coApplicantCreditReportNrOfPaymentRemarks > s:maxNrOfPaymentRemarks";

        public override string GetDisplayName(string country, string language) => "Max allowed co applicant payment remarks";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context)
        {
            if (context.Variables.GetString("coApplicantCreditReportNrOfPaymentRemarks", true) == "noCoApp")
                return false;
            return context.Variables.GetIntRequired("coApplicantCreditReportNrOfPaymentRemarks") > context.StaticParameters.GetInt("maxNrOfPaymentRemarks");
        }

        public override string DefaultRejectionReasonName => "paymentRemark";
    }
}