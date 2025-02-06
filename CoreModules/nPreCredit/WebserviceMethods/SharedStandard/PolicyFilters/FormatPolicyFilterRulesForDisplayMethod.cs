using Newtonsoft.Json;
using nPreCredit.Code.StandardPolicyFilters;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class FormatPolicyFilterRulesForDisplayMethod : TypedWebserviceMethod<FormatPolicyFilterRulesForDisplayMethod.Request, FormatPolicyFilterRulesForDisplayMethod.Response>
    {
        public override string Path => "LoanStandard/PolicyFilters/Format-Rules-For-Display";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled || NEnv.IsStandardMortgageLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("High");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var displayFormattedRulesByKey = new Dictionary<string, Response.Rule>();

            var country = request.Country ?? NEnv.ClientCfg.Country.BaseCountry;
            var displayLanguage = request.DisplayLanguage ?? "en";

            foreach (var keyAndValue in request.RulesByKey ?? new Dictionary<string, RuleAndStaticParameterValues>())
            {
                var key = keyAndValue.Key;
                var ruleWithValue = keyAndValue.Value;

                var rule = RuleFactory.GetRuleByName(ruleWithValue.RuleName);
                var staticParameters = StaticParameterSet.CreateFromStoredValues(ruleWithValue.StaticParameterValues?.StoredValues);
                displayFormattedRulesByKey[key] = new Response.Rule
                {
                    RuleName = rule.Name,
                    RuleDisplayName = rule.GetDisplayName(country, displayLanguage),
                    Description = rule.GetDescription(country, displayLanguage),
                    StaticParametersDisplayWithNames = rule.GetStaticParametersDisplay(country, displayLanguage, staticParameters, true),
                    StaticParametersDisplayWithoutNames = rule.GetStaticParametersDisplay(country, displayLanguage, staticParameters, false)
                };
            }

            return new Response
            {
                DisplayFormattedRulesByKey = displayFormattedRulesByKey
            };
        }

        public class Request
        {
            [Required]
            public Dictionary<string, RuleAndStaticParameterValues> RulesByKey { get; set; }

            public string Country { get; set; }

            public string DisplayLanguage { get; set; }
        }

        public class Response
        {
            public Dictionary<string, Rule> DisplayFormattedRulesByKey { get; set; }

            public class Rule
            {
                public string RuleName { get; set; }
                public string RuleDisplayName { get; set; }
                public string Description { get; set; }
                public string StaticParametersDisplayWithNames { get; set; }
                public string StaticParametersDisplayWithoutNames { get; set; }
            }
        }
    }
}