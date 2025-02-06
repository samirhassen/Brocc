using nPreCredit.Code.StandardPolicyFilters;
using System.Collections.Generic;

namespace nPreCredit.Code
{
    /// <summary>
    /// BEWARE: If you change this make sure to increase the version for both mortgage loans and unsecure loans
    /// </summary>
    public abstract class StandardCreditRecommendationModelBase
    {
        /// <summary>
        /// Raw result from the policy filter engine
        /// </summary>
        public EngineResult PolicyFilterResult { get; set; }

        /// <summary>
        /// Policy filter result per rule formatted for ui rendering. Stored so we can render the details
        /// on both new and view and even if the rules have later been changed or removed.
        /// </summary>
        public List<PolicyFilterDetailsDisplayItem> PolicyFilterDetailsDisplayItems { get; set; }


        public class PolicyFilterDetailsDisplayItem
        {
            public string RuleName { get; set; }
            public string RuleDisplayName { get; set; }
            public int? ForApplicantNr { get; set; }
            public string StaticParametersDisplayText { get; set; }
            public string VariablesDisplayText { get; set; }
            public bool? IsRejectedByRule { get; set; }
            public bool IsSkipped { get; set; }
            public string PhaseDisplayName { get; set; }
            public string PhaseName { get; set; }
            public bool IsManualControlPhase { get; set; }
        }

        private static string GetPhaseDisplayName(string phaseName, string clientCountry, string displayLanguage)
        {
            if (phaseName == "ManualControl")
                return "Manual";
            else
                return phaseName;
        }

        public static List<PolicyFilterDetailsDisplayItem> CreateRuleDisplayItems(EngineResult policyFilterResult, string clientCountry, string displayLanguage)
        {
            if (policyFilterResult == null)
                return null;

            var items = new List<PolicyFilterDetailsDisplayItem>();
            void HandlePhase(PhaseResult phaseResult)
            {
                if (phaseResult == null) return;

                foreach (var ruleResult in phaseResult.RuleResults)
                {
                    var r = RuleFactory.GetRuleByName(ruleResult.RuleName);
                    items.Add(new PolicyFilterDetailsDisplayItem
                    {
                        RuleName = r.Name,
                        PhaseName = phaseResult.PhaseName,
                        PhaseDisplayName = GetPhaseDisplayName(phaseResult.PhaseName, clientCountry, displayLanguage),
                        RuleDisplayName = r.GetDisplayName(clientCountry, displayLanguage),
                        ForApplicantNr = ruleResult.ForApplicantNr,
                        StaticParametersDisplayText = r.GetStaticParametersDisplay(clientCountry, displayLanguage, ruleResult.StaticParameters, false),
                        VariablesDisplayText = r.GetVariableDisplay(clientCountry, displayLanguage, ruleResult.GetScopedVariables(policyFilterResult.VariableSet)),
                        IsRejectedByRule = ruleResult.IsRejectedByRule,
                        IsSkipped = ruleResult.IsSkipped,
                        IsManualControlPhase = phaseResult.IsManualControlPhase
                    });
                }
            }

            HandlePhase(policyFilterResult?.InternalResult);
            HandlePhase(policyFilterResult?.ExternalResult);
            HandlePhase(policyFilterResult?.ManualControlResult);

            return items;
        }
    }
}