using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.StandardPolicyFilters
{
    public class RuleFactory
    {
        private static Lazy<Dictionary<string, Rule>> reflectedRules = new Lazy<Dictionary<string, Rule>>(() =>
        {
            var ruleTypes = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => !p.IsAbstract && p.IsSubclassOfRawGeneric(typeof(Rule)))
                .ToList();

            var rules = new List<Rule>();
            foreach (var ruleType in ruleTypes)
            {
                try
                {
                    rules.Add((Rule)ruleType.GetConstructor(Type.EmptyTypes).Invoke(null));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Rule {ruleType.FullName} seems broken", ex);
                }
            }

            var dupes = rules.GroupBy(x => x.Name).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
            if (dupes.Count > 0)
                throw new Exception("These rulenames occur on more than one rule class: " + string.Join(", ", dupes));

            return rules.ToDictionary(x => x.Name, x => x);
        });

        /// <summary>
        /// Gets any rule using reflection
        /// </summary>
        /// <param name="ruleName">case sensetive</param>
        public static Rule GetRuleByName(string ruleName)
        {
            if (!reflectedRules.Value.ContainsKey(ruleName))
                throw new Exception($"No such rule: {ruleName}");
            return reflectedRules.Value[ruleName];
        }

        public static ICollection<string> GetAllRuleNames() => reflectedRules.Value.Keys;

        public static ICollection<string> GetProductFilteredRuleNames(IPreCreditEnvSettings envSettings)
        {
            var rulesNames = GetAllRuleNames().ToList();
            if (envSettings.IsStandardMortgageLoansEnabled)
                rulesNames = rulesNames.Where(x => !NamesDisabledForMortgageLoans.Contains(x)).ToList();

            if (envSettings.IsStandardUnsecuredLoansEnabled)
                rulesNames = rulesNames.Where(x => !NamesDisabledForUnsecuredLoans.Contains(x)).ToList();

            return rulesNames;
        }

        private static ISet<string> NamesDisabledForMortgageLoans => new HashSet<string>
        {
            "MaxAllowedDbr", //Since the datasource does not have debtBurdenRatio. Can be relaxed if that is added.
            "MinAllowedSettlementInterestRate", //Other loans settlement rules not applicable for ml
            "MinAllowedWeightedAverageSettlementInterestRate" //Other loans settlement rules not applicable for ml
        };

        private static ISet<string> NamesDisabledForUnsecuredLoans => new HashSet<string>
        {
            "BannedPropertyZipCode",
            "AllowedPropertyZipCode",
            "MaxAllowedLti", //Since the datasource does not have loanToIncome.  Can be relaxed if that is added.
            "MaxAllowedLtvPercent" //Since there is no collateral this is fundamentally not useful for unsecured loans.
        };
    }
}