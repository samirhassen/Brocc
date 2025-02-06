using System;

namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class MaxInternalVsExternalIncomePercentDifferenceRule : Rule
    {
        public override string Name => "MaxInternalVsExternalIncomePercentDifference";

        public override bool IsEvaluatedPerApplicant => true;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables();

        public override string[] RequestedApplicantLevelVaribles =>
            CreateVariables("applicantCreditReportIncomePerMonth", "applicantIncomePerMonth");

        public override StaticRuleParameter[] StaticParameters =>
            CreateParameters(CreatePercentStaticParameter("maxAllowedIncomeDiffPercent"));

        public override string GetDescription(string country, string language) =>
            "100 * ABS(v:applicantCreditReportIncomePerMonth - v:applicantIncomePerMonth) / MAX(ABS(v:applicantIncomePerMonth), ABS(v:applicantCreditReportIncomePerMonth)) < s:maxAllowedIncomeDiffPercent";

        public override string GetDisplayName(string country, string language) =>
            "Max percent diff external vs internal income";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context)
        {
            var computedDiffPercent = ComputeDiffPercent(context.Variables, true);
            return computedDiffPercent > context.StaticParameters.GetInt("maxAllowedIncomeDiffPercent");
        }

        public override string GetVariableDisplay(string country, string uiLanguage, ScopedVariableSet variables)
        {
            var computedDiffPercent = variables == null ? null : ComputeDiffPercent(variables, false);
            if (computedDiffPercent == null)
                return null;
            var percentDisplay = (computedDiffPercent.Value / 100m).ToString("P", GetFormattingCulture(country, uiLanguage));
            var creditReportIncome = variables.GetIntRequired("applicantCreditReportIncomePerMonth");
            var applicationIncome = variables.GetIntRequired("applicantIncomePerMonth");

            return $"{percentDisplay} ({applicationIncome} vs {creditReportIncome})";
        }

        private decimal? ComputeDiffPercent(ScopedVariableSet variables, bool require)
        {
            Func<string, int?> get = x => require ? variables.GetIntRequired(x) : variables.GetIntOptional(x);

            var creditReportIncomeRaw = get("applicantCreditReportIncomePerMonth");
            var selfReportedIncomeRaw = get("applicantIncomePerMonth");

            if (!creditReportIncomeRaw.HasValue || !selfReportedIncomeRaw.HasValue)
                return null;

            var creditReportIncome = creditReportIncomeRaw.Value;
            var selfReportedIncome = selfReportedIncomeRaw.Value;
            var incomeDiff = Math.Abs(creditReportIncome - selfReportedIncome);

            if (incomeDiff == 0)
                return 0;

            return Math.Round(100m * incomeDiff / Math.Max(Math.Abs(creditReportIncome), Math.Abs(selfReportedIncome)), 2);
        }
    }
}