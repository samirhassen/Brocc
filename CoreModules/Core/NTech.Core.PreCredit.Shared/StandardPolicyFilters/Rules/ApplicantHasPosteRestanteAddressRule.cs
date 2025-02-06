namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class ApplicantHasPosteRestanteAddressRule : Rule
    {
        public override string Name => "ApplicantHasPosteRestanteAddress";

        public override bool IsEvaluatedPerApplicant => true;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables();

        public override string[] RequestedApplicantLevelVaribles => CreateVariables("applicantHasPosteRestanteAddress");

        public override StaticRuleParameter[] StaticParameters => CreateParameters();

        public override string GetDescription(string country, string language) =>
            "v:applicantHasPosteRestanteAddress = true";

        public override string GetDisplayName(string country, string language) =>
            "Applicant has poste restante address";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetBoolRequired("applicantHasPosteRestanteAddress");

        public override string DefaultRejectionReasonName => "address";
    }
}