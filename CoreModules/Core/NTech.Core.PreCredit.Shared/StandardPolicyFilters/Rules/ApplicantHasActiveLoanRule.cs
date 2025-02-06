namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class ApplicantHasActiveLoanRule : Rule
    {
        public override string Name => "ApplicantHasActiveLoan";
        public override bool IsEvaluatedPerApplicant => true;
        public override string[] RequestedApplicationLevelVaribles => CreateVariables();
        public override string[] RequestedApplicantLevelVaribles => CreateVariables("applicantHasLoanInSystem");
        public override StaticRuleParameter[] StaticParameters => CreateParameters();

        public override string GetDisplayName(string country, string language) =>
            "Applicant has active loan";

        public override string GetDescription(string country, string language) => "v:applicantHasActiveLoanInSystem = Yes";

        public override string GetVariableDisplay(string country, string uiLanguage, ScopedVariableSet variables) =>
            GetBooleanDisplayValue("applicantHasLoanInSystem", uiLanguage, variables);

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetBoolRequired("applicantHasLoanInSystem");


    }
}