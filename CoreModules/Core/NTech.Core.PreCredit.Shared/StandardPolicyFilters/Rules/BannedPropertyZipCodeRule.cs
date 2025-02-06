using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class BannedPropertyZipCodeRule : Rule
    {
        public override string Name => "BannedPropertyZipCode";

        public override bool IsEvaluatedPerApplicant => false;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables("objectZipCode");

        public override string[] RequestedApplicantLevelVaribles => CreateVariables();

        public override StaticRuleParameter[] StaticParameters => CreateParameters(CreateStringStaticParameter("bannedZipCodesExpression"));

        public override string GetDescription(string country, string language) => "v:objectZipCode matches s:bannedZipCodesExpression";


        public override string GetDisplayName(string country, string language) => "Banned zipcodes";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context)
        {
            var expression = context.StaticParameters.GetString("bannedZipCodesExpression");
            var objectZipCode = context.Variables.GetString("objectZipCode", false);
            if (!IsZipCodeExpressionValid(expression) || objectZipCode == null)
                return null;

            return DoesZipCodeMatchExpression(expression, objectZipCode);
        }

        public override string GetStaticParametersDisplay(string country, string uiLanguage, StaticParameterSet staticParameters, bool includeParameterNames)
        {
            if (StaticParameters == null || staticParameters == null)
                return null;

            var parameter = StaticParameters[0];

            var expression = staticParameters?.GetString(parameter.Name);
            var displayValue = IsZipCodeExpressionValid(expression) ? expression : $"INVALID({expression})";

            return includeParameterNames ? $"s:{parameter.Name}={displayValue}" : displayValue;
        }

        public static bool DoesZipCodeMatchExpression(string expression, string zipCode)
        {
            var match = CreateMatcher(expression);
            if (match == null)
                return false;

            return match(zipCode);
        }

        public static bool IsZipCodeExpressionValid(string expression) => CreateMatcher(expression) != null;

        private const int ZipCodeLength = 5;

        /// <summary>
        /// Grammar:
        /// CODE = SINGLE | LIST
        /// LIST = SINGLE COMMA LIST | SINGLE COMMA SINGLE
        /// SINGLE = RANGE | PATTERN
        /// RANGE = PATTERN MINUS PATTERN
        /// PATTERN = D* | DD* | DDD* | ...
        /// D = 0 - 9
        /// 
        /// So like:
        /// 99*           matches 99000, 99100, 99712 but not say 98100, 98100, 12345
        /// 12*-231*      matches anything in the range 12000 -> 23199
        /// 99*,12*-231*  matches anything that matches either of the two
        /// </summary>
        private static Func<string, bool> CreateMatcher(string expression)
        {
            if (expression == null)
                return null;

            expression = expression.Replace(" ", ""); //Ignore inner whitespace

            var singleExpressions = expression.Split(',');
            var matchers = new List<Func<string, bool>>(singleExpressions.Length);
            foreach (var singleExpression in singleExpressions)
            {
                var singleMatcher = CreateSingleMatcher(singleExpression);
                if (singleMatcher == null)
                    return null;
                matchers.Add(singleMatcher);
            }
            return x => matchers.Any(isMatch => isMatch(x));
        }

        private static int? ParseZipCode(string zipCode)
        {
            var z = (zipCode ?? "").Trim();
            if (z.Length != ZipCodeLength)
                return null;
            if (!int.TryParse(zipCode, out var zipCodeInt))
                return null;
            return zipCodeInt;
        }

        private static Func<string, bool> CreateSingleMatcher(string expression)
        {
            const string Pattern = "([0-9]+(?:\\*)?)";
            var rangeMatch = Regex.Match(expression, $"^{Pattern}\\-{Pattern}$");
            if (rangeMatch.Success)
            {
                var from = rangeMatch.Groups[1].Value;
                if (from.EndsWith("*"))
                    from = from.Replace("*", "").PadRight(ZipCodeLength, '0');

                var to = rangeMatch.Groups[2].Value;
                if (to.EndsWith("*"))
                    to = to.Replace("*", "").PadRight(ZipCodeLength, '9');

                if (from.Length != ZipCodeLength || to.Length != ZipCodeLength)
                    return null;

                var fromInt = int.Parse(from);
                var toInt = int.Parse(to);

                return zipCode =>
                {
                    var zipCodeInt = ParseZipCode(zipCode);
                    return zipCodeInt >= fromInt && zipCodeInt <= toInt;
                };
            }

            var singleMatch = Regex.Match(expression, $"^{Pattern}$");
            if (!singleMatch.Success)
                return null;

            //So DD* basically means DD[0-9][0-9][0-9] which is equivalent to DD*-DD* since that gets translated to DD000-DD999
            return CreateSingleMatcher($"{expression}-{expression}");
        }

        private static Func<string, bool> CreateSingleaaMatcher(string expression)
        {
            const char WildCardMarker = 'x';
            expression = (expression?.Trim() ?? "").ToLowerInvariant();
            if (expression.Length != ZipCodeLength)
                return null;

            var prefixExpression = "";
            var hasPassedPrefix = false;
            for (var i = 0; i < expression.Length; ++i)
            {
                if (Char.IsDigit(expression[i]))
                {
                    if (hasPassedPrefix)
                        return null;

                    prefixExpression += expression[i];
                }
                else if (expression[i] == WildCardMarker)
                {
                    hasPassedPrefix = true;
                }
                else
                {
                    return null;
                }
            }

            return x => x.Length == expression.Length && x.StartsWith(prefixExpression);
        }
    }
}