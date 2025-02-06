using System.Text;
using System;
using Newtonsoft.Json;

namespace nPreCredit.Code.StandardPolicyFilters
{
    public class RuleSet
    {
        public RuleAndStaticParameterValues[] InternalRules { get; set; }
        public RuleAndStaticParameterValues[] ExternalRules { get; set; }
        public RuleAndStaticParameterValues[] ManualControlOnAcceptedRules { get; set; }

        public static RuleSet Create(RuleAndStaticParameterValues[] internalRules, RuleAndStaticParameterValues[] externalRules, RuleAndStaticParameterValues[] manualControlRules) =>
            new RuleSet
            {
                InternalRules = internalRules ?? new RuleAndStaticParameterValues[] { },
                ExternalRules = externalRules ?? new RuleAndStaticParameterValues[] { },
                ManualControlOnAcceptedRules = manualControlRules ?? new RuleAndStaticParameterValues[] { }
            };

        public static bool TryParseImportCode(string code, out RuleSet ruleSet)
        {
            ruleSet = null;
            if (code == null || !code.StartsWith("S_") || !code.EndsWith("_S"))
                return false;

            try
            {
                ruleSet = JsonConvert.DeserializeObject<RuleSet>(Encoding.UTF8.GetString(Convert.FromBase64String(code.Substring(2, code.Length - 4))));

                if (ruleSet.InternalRules == null)
                    ruleSet.InternalRules = new RuleAndStaticParameterValues[] {};
                if(ruleSet.ExternalRules == null)
                    ruleSet.ExternalRules = new RuleAndStaticParameterValues[] { };
                if(ruleSet.ManualControlOnAcceptedRules == null)
                    ruleSet.ManualControlOnAcceptedRules = new RuleAndStaticParameterValues[] { };

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}