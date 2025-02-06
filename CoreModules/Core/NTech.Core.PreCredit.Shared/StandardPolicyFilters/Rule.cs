using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.StandardPolicyFilters
{
    public abstract class Rule
    {
        public abstract string Name { get; }
        public virtual string DefaultRejectionReasonName => "minimumDemands";

        public abstract string GetDisplayName(string country, string language);
        public abstract string GetDescription(string country, string language);

        /// <summary>
        /// Used when showing the scoring result to give the user an easy way to verify the result.
        /// Example:
        /// With the rule MaxAge: v:applicantAgeInYears > s:maxAllowedAge
        /// GetDisplayVariable could be:
        /// context.GetVariable("applicantAgeInYears", false)
        /// For rules that use more than one variable this will not default to anything but a custom implementation can be added if one makes sense.
        /// This will typically be displayed in a small table cell so keep this short.
        /// </summary>
        /// <param name="country">client country (can be used for formatting for instance)</param>
        /// <param name="context">variable set used for scoring</param>
        /// <returns>display value</returns>
        public virtual string GetVariableDisplay(string country, string uiLanguage, ScopedVariableSet variables)
        {
            if (((RequestedApplicantLevelVaribles?.Length ?? 0) + (RequestedApplicationLevelVaribles?.Length ?? 0)) == 1)
            {
                if ((RequestedApplicantLevelVaribles?.Length ?? 0) == 1)
                    return variables.GetString(RequestedApplicantLevelVaribles[0], false);
                else
                    return variables.GetString(RequestedApplicationLevelVaribles[0], false);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Override GetVariableDisplay-method and send in your bool to this method, to ensure all booleans are display in the same way in a more readable format for the user. 
        /// </summary>
        /// <returns>Yes or No</returns>
        protected static string GetBooleanDisplayValue(string variableName, string uiLanguage, ScopedVariableSet variables)
        {
            var result = variables.GetBoolOptional(variableName);
            return result.HasValue ? (result.Value ? "Yes" : "No") : null;
        }

        /// <summary>
        /// Examples of what this should do:
        /// MinAllowedLeftToLiveOn with names:    s:minLeftToLiveOnAmount=18
        /// MinAllowedLeftToLiveOn without names: 18
        /// BannedEmployment with names:          s:bannedEmploymentFormCodes=[Full time,Part time]
        /// BannedEmployment without names:       [Full time,Part time]
        /// </summary>
        public virtual string GetStaticParametersDisplay(string country, string uiLanguage, StaticParameterSet staticParameters, bool includeParameterNames)
        {
            if (StaticParameters == null || staticParameters == null)
                return null;

            return string.Join(" ", StaticParameters.Select(parameter =>
            {
                var displayValue = parameter.GetDisplayValue(country, uiLanguage, staticParameters);
                return includeParameterNames ? $"s:{parameter.Name}={displayValue}" : displayValue;
            }));
        }

        public RuleResult EvaluateRule(EvaluateRuleContext context)
        {
            try
            {
                var evaluateResult = IsRejectedByRule(context);
                return new RuleResult(Name, context.Variables.ForApplicantNr, evaluateResult, context.StaticParameters);
            }
            catch (PolicyFilterException ex)
            {
                if (ex.IsMissingApplicantLevelVariable || ex.IsMissingApplicationLevelVariable)
                {
                    return new RuleResult(Name, context.Variables.ForApplicantNr, null, context.StaticParameters)
                    {
                        IsMissingApplicantLevelVariable = ex.IsMissingApplicantLevelVariable,
                        IsMissingApplicationLevelVariable = ex.IsMissingApplicationLevelVariable,
                        MissingVariableName = ex.MissingVariableOrParameterName,
                        MissingApplicantLevelApplicantNrs = ex.MissingApplicantLevelApplicantNrs
                    };
                }
                else
                    throw;
            }
        }
        protected abstract bool? IsRejectedByRule(EvaluateRuleContext context);
        public abstract bool IsEvaluatedPerApplicant { get; }
        /// <summary>
        /// example:
        /// public override string[] RequestedApplicationLevelVaribles => RequestVariables("leftToLiveOn");
        /// </summary>
        public abstract string[] RequestedApplicationLevelVaribles { get; }
        /// <summary>
        /// example:
        /// public override string[] RequestedApplicantLevelVaribles => RequestVariables("incomePerMonth");
        /// </summary>
        public abstract string[] RequestedApplicantLevelVaribles { get; }
        protected string[] CreateVariables(params string[] names) => names;

        /// <summary>
        /// Example:
        /// public override StaticRuleParameter[] StaticParameters => CreateParameters(CreateIntStaticParameter("minLeftToLiveOn"));
        /// </summary>
        public abstract StaticRuleParameter[] StaticParameters { get; }
        protected StaticRuleParameter[] CreateParameters(params StaticRuleParameter[] parameters) => parameters;
        protected (string Value, Func<string, string> GetDisplayNameOrNull) CreateParameterOption(string value, Func<string, string> getDisplayNameOrNull = null) =>
            (Value: value, GetDisplayNameOrNull: getDisplayNameOrNull ?? (x => value));

        public static System.Globalization.CultureInfo GetFormattingCulture(string country, string uiLanguage)
        {
            string culture = "sv-SE";
            if (country == "FI")
                culture = "fi-FI";
            return System.Globalization.CultureInfo.GetCultureInfo(culture);
        }

        protected StaticRuleParameter CreateIntStaticParameter(string name, Func<string, string> getDisplayNameOrNull = null) =>
            new StaticRuleParameter(name, StaticRuleParameter.StaticParameterTypeCode.Int, getDisplayNameOrNull: getDisplayNameOrNull);

        protected StaticRuleParameter CreateDecimalStaticParameter(string name, Func<string, string> getDisplayNameOrNull = null) =>
            new StaticRuleParameter(name, StaticRuleParameter.StaticParameterTypeCode.Decimal, getDisplayNameOrNull: getDisplayNameOrNull);

        protected StaticRuleParameter CreatePercentStaticParameter(string name, Func<string, string> getDisplayNameOrNull = null) =>
            new StaticRuleParameter(name, StaticRuleParameter.StaticParameterTypeCode.Percent, getDisplayNameOrNull: getDisplayNameOrNull);

        protected StaticRuleParameter CreateStringStaticParameter(string name, Func<string, string> getDisplayNameOrNull = null) =>
            new StaticRuleParameter(name, StaticRuleParameter.StaticParameterTypeCode.String, getDisplayNameOrNull: getDisplayNameOrNull);

        public StaticRuleParameter CreateStringListStaticParameter(string name,
                List<(string Value, Func<string, string> GetDisplayNameOrNull)> options = null, Func<string, string> getDisplayNameOrNull = null) =>
            new StaticRuleParameter(name, StaticRuleParameter.StaticParameterTypeCode.String, isList: true, options: options, getDisplayNameOrNull: getDisplayNameOrNull);
    }
}