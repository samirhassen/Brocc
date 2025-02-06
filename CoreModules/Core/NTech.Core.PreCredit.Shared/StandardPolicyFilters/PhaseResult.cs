using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.StandardPolicyFilters
{
    public class PhaseResult
    {
        public string PhaseName { get; set; }
        public List<RuleResult> RuleResults { get; set; }
        public bool IsRejectedByAnyRule() => RuleResults.Any(x => x.IsRejectedByRule == true);
        public bool IsManualControlPhase { get; set; }
        public List<string> RejectionReasonNames { get; set; }
    }
}