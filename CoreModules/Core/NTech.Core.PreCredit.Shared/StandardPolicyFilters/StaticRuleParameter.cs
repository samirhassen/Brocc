using System;
using System.Collections.Generic;

namespace nPreCredit.Code.StandardPolicyFilters
{
    public class StaticRuleParameter
    {
        public StaticRuleParameter(
            string name,
            StaticParameterTypeCode typeCode,
            bool isList = false,
            List<(string Value, Func<string, string> GetDisplayNameOrNull)> options = null,
            Func<string, string> getDisplayNameOrNull = null)
        {
            IsList = isList;
            Name = name;
            TypeCode = typeCode;
            this.getDisplayNameOrNull = getDisplayNameOrNull;
            if (options == null)
                Options = null;
            else
            {
                Options = new List<(string Value, Func<string, string> GetDisplayName)>();
                foreach (var opt in options)
                {
                    Options.Add((Value: opt.Value, GetDisplayName: x => GetDisplayNameComposed(x, opt.GetDisplayNameOrNull)));
                }
            }
        }
        public string Name { get; }
        public bool IsList { get; }
        public StaticParameterTypeCode TypeCode { get; }
        public List<(string Value, Func<string, string> GetDisplayName)> Options { get; set; }
        private Func<string, string> getDisplayNameOrNull;
        public string GetDisplayName(string code) => GetDisplayNameComposed(code, getDisplayNameOrNull);

        private static string GetDisplayNameComposed(string code, Func<string, string> getDisplayNameExternal)
        {
            var value = getDisplayNameExternal?.Invoke(code);
            return string.IsNullOrWhiteSpace(value) ? code : value;
        }

        public enum StaticParameterTypeCode
        {
            Int,
            String,
            Decimal,
            Percent //Stored exactly like decimal but with display format differences
        }

        public string GetDisplayValue(string country, string uiLanguage, StaticParameterSet staticParameters)
        {
            if (IsList)
            {
                switch (TypeCode)
                {
                    case StaticParameterTypeCode.String:
                        return FormatListForDisplay(staticParameters.GetStringList(Name));
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                switch (TypeCode)
                {
                    case StaticParameterTypeCode.String:
                        return staticParameters.GetString(Name);
                    case StaticParameterTypeCode.Percent:
                        return FormatPercentForDisplay(staticParameters.GetDecimal(Name), country, uiLanguage);
                    case StaticParameterTypeCode.Int:
                        return (staticParameters.GetInt(Name)).ToString(Rule.GetFormattingCulture(country, uiLanguage));
                    case StaticParameterTypeCode.Decimal:
                        return (staticParameters.GetDecimal(Name)).ToString(Rule.GetFormattingCulture(country, uiLanguage));
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public static string FormatListForDisplay(IEnumerable<string> displayValues) => $"[{string.Join(",", displayValues)}]";
        public static string FormatPercentForDisplay(decimal? value, string country, string uiLanguage) =>
            value.HasValue ? (value.Value / 100m).ToString("P", Rule.GetFormattingCulture(country, uiLanguage)) : null;
    }
}