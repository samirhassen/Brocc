namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class AllowedPropertyZipCodeRule : Rule
    {
        public override string Name => "AllowedPropertyZipCode";

        public override bool IsEvaluatedPerApplicant => false;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables("objectZipCode");

        public override string[] RequestedApplicantLevelVaribles => CreateVariables();

        public override StaticRuleParameter[] StaticParameters => CreateParameters(CreateStringStaticParameter("allowedZipCodesExpression"));

        public override string GetDescription(string country, string language) => "not (v:objectZipCode matches s:allowedZipCodesExpression)";


        public override string GetDisplayName(string country, string language) => "Allowed zipcodes";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context)
        {
            var expression = context.StaticParameters.GetString("allowedZipCodesExpression");
            var objectZipCode = context.Variables.GetString("objectZipCode", false);
            if (!BannedPropertyZipCodeRule.IsZipCodeExpressionValid(expression) || objectZipCode == null)
                return null;

            return !BannedPropertyZipCodeRule.DoesZipCodeMatchExpression(expression, objectZipCode);
        }

        public override string GetStaticParametersDisplay(string country, string uiLanguage, StaticParameterSet staticParameters, bool includeParameterNames)
        {
            if (StaticParameters == null || staticParameters == null)
                return null;

            var parameter = StaticParameters[0];

            var expression = staticParameters?.GetString(parameter.Name);
            var displayValue = BannedPropertyZipCodeRule.IsZipCodeExpressionValid(expression) ? expression : $"INVALID({expression})";

            return includeParameterNames ? $"s:{parameter.Name}={displayValue}" : displayValue;
        }
    }
}