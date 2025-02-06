namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class BannedLoanObjectiveRule : Rule
    {
        public override string Name => "BannedLoanObjectiveRule";

        public override bool IsEvaluatedPerApplicant => false;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables("loanObjective");

        public override string[] RequestedApplicantLevelVaribles => CreateVariables();

        public override StaticRuleParameter[] StaticParameters => CreateParameters(CreateStringStaticParameter("bannedLoanObjective"));

        public override string GetDescription(string country, string language) => "v:loanObjective = s:bannedLoanObjective";

        public override string GetDisplayName(string country, string language) => "Banned loan objective";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context)
        {
            var bannedLoanObjective = context.StaticParameters.GetString("bannedLoanObjective");
            var loanObjective = context.Variables.GetString("loanObjective", false);
            if (loanObjective == null)
                return null;

            return loanObjective.EqualsIgnoreCase(bannedLoanObjective);
        }
    }
}