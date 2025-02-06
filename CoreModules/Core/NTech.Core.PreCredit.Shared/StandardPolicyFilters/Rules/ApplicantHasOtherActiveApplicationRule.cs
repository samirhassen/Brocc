namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class ApplicantHasOtherActiveApplicationRule : Rule
    {
        public override string Name => "ApplicantHasOtherActiveApplication";
        public override bool IsEvaluatedPerApplicant => true;
        public override string[] RequestedApplicationLevelVaribles => CreateVariables();
        public override string[] RequestedApplicantLevelVaribles => CreateVariables("applicantHasOtherActiveApplicationsInSystem");
        public override StaticRuleParameter[] StaticParameters => CreateParameters();

        public override string GetDescription(string country, string language) => "v:applicantHasOtherActiveApplicationsInSystem = Yes";

        public override string GetDisplayName(string country, string language) =>
            "Applicant has other active application";
        public override string GetVariableDisplay(string country, string uiLanguage, ScopedVariableSet variables) =>
            GetBooleanDisplayValue("applicantHasOtherActiveApplicationsInSystem", uiLanguage, variables);

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetBoolRequired("applicantHasOtherActiveApplicationsInSystem");

        public override string DefaultRejectionReasonName => "alreadyApplied";
    }
}