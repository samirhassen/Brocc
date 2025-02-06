using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.StandardPolicyFilters
{
    public class PolicyFilterEngine
    {
        public EngineResult Evaluate(RuleSet ruleSet, IPolicyFilterDataSource dataSource)
        {
            VariableSet variables = null;
            var loadedApplicantVaribles = new HashSet<string>();
            var loadedApplicationVariables = new HashSet<string>();
            void LoadVariables(IEnumerable<string> applicationNames, IEnumerable<string> applicantNames)
            {
                var newApplicationVariableNames = applicationNames.Except(loadedApplicationVariables).ToHashSetShared();
                var newApplicantVariableNames = applicantNames.Except(loadedApplicantVaribles).ToHashSetShared();
                loadedApplicationVariables.AddRange(newApplicationVariableNames);
                loadedApplicantVaribles.AddRange(newApplicantVariableNames);
                var phaseVariables = dataSource.LoadVariables(newApplicationVariableNames, newApplicantVariableNames);
                if (variables == null)
                    variables = phaseVariables;
                else
                    variables.SetValues(phaseVariables);
            }

            var hasPreviousRejectedPhase = false;
            PhaseResult EvaluatePhaseLocal(RuleAndStaticParameterValues[] rules, string phaseName, bool isManualControlPhase)
            {
                if (hasPreviousRejectedPhase)
                {
                    return SkipPhase(rules, variables.GetApplicantNrs(), phaseName, isManualControlPhase);
                }
                else
                {
                    LoadVariables(
                        rules.SelectMany(x => RuleFactory.GetRuleByName(x.RuleName).RequestedApplicationLevelVaribles),
                        rules.SelectMany(x => RuleFactory.GetRuleByName(x.RuleName).RequestedApplicantLevelVaribles));

                    var phaseResult = EvaluatePhase(rules, variables, phaseName, isManualControlPhase);
                    if (phaseResult.IsRejectedByAnyRule())
                        hasPreviousRejectedPhase = true;
                    return phaseResult;
                }
            }
            var internalPhaseResult = EvaluatePhaseLocal(ruleSet.InternalRules ?? new RuleAndStaticParameterValues[] { }, InternalPhaseName, false);
            var externalPhaseResult = EvaluatePhaseLocal(ruleSet.ExternalRules ?? new RuleAndStaticParameterValues[] { }, ExternalPhaseName, false);
            var manualControlResult = EvaluatePhaseLocal(ruleSet.ManualControlOnAcceptedRules ?? new RuleAndStaticParameterValues[] { }, ManualControlPhaseName, true);

            bool? isAcceptRecommended = null;
            bool? isManualControlRecommended = null;

            var allPhases = new[] { internalPhaseResult, externalPhaseResult, manualControlResult };

            List<string> rejectionReasonNames = null;
            if (allPhases.Any(x => x.IsRejectedByAnyRule() && !x.IsManualControlPhase))
            {
                isAcceptRecommended = false;
                rejectionReasonNames = allPhases
                    .Where(x => !x.IsManualControlPhase && x.RejectionReasonNames != null)
                    .SelectMany(x => x.RejectionReasonNames)
                    .Distinct()
                    .ToList();
            }
            else
            {
                //NOTE: There is an edgecase where no rule at all was evaluated where you could debate if accept is really a reasonable recommendation but keeping it for now
                isAcceptRecommended = true;
                isManualControlRecommended = allPhases.Any(x => x.IsRejectedByAnyRule() && x.IsManualControlPhase);
            }

            return new EngineResult
            {
                InternalResult = internalPhaseResult,
                ExternalResult = externalPhaseResult,
                ManualControlResult = manualControlResult,
                IsAcceptRecommended = isAcceptRecommended,
                IsManualControlRecommended = isManualControlRecommended,
                VariableSet = variables,
                RejectionReasonNames = rejectionReasonNames
            };
        }

        private PhaseResult SkipPhase(RuleAndStaticParameterValues[] rules, List<int> applicantNrs, string phaseName, bool isManualControlPhase)
        {
            var result = new PhaseResult
            {
                RuleResults = new List<RuleResult>(),
                PhaseName = phaseName,
                IsManualControlPhase = isManualControlPhase
            };

            foreach (var ruleTemplate in rules)
            {
                var rule = RuleFactory.GetRuleByName(ruleTemplate.RuleName);
                if (rule.IsEvaluatedPerApplicant)
                {
                    foreach (var applicantNr in applicantNrs)
                    {
                        result.RuleResults.Add(new RuleResult(rule.Name, applicantNr, null, ruleTemplate.StaticParameterValues) { IsSkipped = true });
                    }
                }
                else
                {
                    result.RuleResults.Add(new RuleResult(rule.Name, null, null, ruleTemplate.StaticParameterValues) { IsSkipped = true });
                }
            }

            return result;
        }

        private PhaseResult EvaluatePhase(RuleAndStaticParameterValues[] rules, VariableSet variables, string phaseName, bool isManualControlPhase)
        {
            var result = new PhaseResult
            {
                PhaseName = phaseName,
                IsManualControlPhase = isManualControlPhase,
                RuleResults = new List<RuleResult>()
            };

            var rejectionReasonNames = new HashSet<string>();
            void AddRejectionReason(Rule rule, RuleResult ruleResult, RuleAndStaticParameterValues ruleTemplate)
            {
                if (ruleResult.IsRejectedByRule != true) return;

                var reason = ruleTemplate.RejectionReasonName ?? rule.DefaultRejectionReasonName;
                if (reason != null)
                    rejectionReasonNames.Add(reason);
            }

            foreach (var ruleTemplate in rules)
            {
                var rule = RuleFactory.GetRuleByName(ruleTemplate.RuleName);
                if (rule.IsEvaluatedPerApplicant)
                {
                    foreach (var applicantNr in variables.GetApplicantNrs())
                    {
                        var scopedVariables = new ScopedVariableSet(variables, applicantNr);
                        var context = new EvaluateRuleContext(scopedVariables, ruleTemplate.StaticParameterValues);
                        var ruleResult = rule.EvaluateRule(context);
                        AddRejectionReason(rule, ruleResult, ruleTemplate);
                        result.RuleResults.Add(ruleResult);
                    }
                }
                else
                {
                    var scopedVariables = new ScopedVariableSet(variables, null);
                    var context = new EvaluateRuleContext(scopedVariables, ruleTemplate.StaticParameterValues);
                    var ruleResult = rule.EvaluateRule(context);
                    AddRejectionReason(rule, ruleResult, ruleTemplate);
                    result.RuleResults.Add(ruleResult);
                }
            }

            result.RejectionReasonNames = rejectionReasonNames.ToList();

            return result;
        }

        public const string InternalPhaseName = "Internal";
        public const string ExternalPhaseName = "External";
        public const string ManualControlPhaseName = "ManualControl";
    }
}