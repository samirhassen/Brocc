using NTech.Services.Infrastructure.CreditStandard;
using System.Linq;

namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class BannedEmploymentRule : Rule
    {
        public override string Name => "BannedEmployment";

        public override bool IsEvaluatedPerApplicant => true;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables();

        public override string[] RequestedApplicantLevelVaribles => CreateVariables("applicantEmploymentFormCode");

        public override StaticRuleParameter[] StaticParameters => CreateParameters(CreateStringListStaticParameter("bannedEmploymentFormCodes",
             options: CreditStandardEmployment.Codes.Select(x => CreateParameterOption(x.ToString(), y => CreditStandardEmployment.GetDisplayName(x.ToString(), y))).ToList()));

        public override string GetDescription(string country, string language) => "v:applicantEmploymentFormCode in s:bannedEmploymentFormCodes";

        public override string GetDisplayName(string country, string language) => "Banned employment forms";

        public override string GetVariableDisplay(string country, string uiLanguage, ScopedVariableSet variables)
        {
            var code = variables.GetString("applicantEmploymentFormCode", false);
            if (code == null)
                return base.GetVariableDisplay(country, uiLanguage, variables);
            return CreditStandardEmployment.GetDisplayName(code, uiLanguage);
        }

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) => context
            .StaticParameters.GetStringList("bannedEmploymentFormCodes").Contains(context.Variables.GetString("applicantEmploymentFormCode", true));

        public override string GetStaticParametersDisplay(string country, string uiLanguage, StaticParameterSet staticParameters, bool includeParameterNames)
        {
            if (StaticParameters == null || staticParameters == null)
                return null;

            var parameter = StaticParameters[0];

            var items = staticParameters?.GetStringList(parameter.Name);
            var displayValue = items == null ? "-" : StaticRuleParameter.FormatListForDisplay(items.Select(x => CreditStandardEmployment.GetDisplayName(x, uiLanguage)));
            return includeParameterNames ? $"s:{parameter.Name}={displayValue}" : displayValue;
        }
    }
}