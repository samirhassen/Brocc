using System.Collections.Generic;

namespace nPreCredit.Code.StandardPolicyFilters
{
    public class EngineResult
    {
        public PhaseResult InternalResult { get; set; }
        public PhaseResult ExternalResult { get; set; }
        public PhaseResult ManualControlResult { get; set; }
        public VariableSet VariableSet { get; set; }

        public bool? IsAcceptRecommended { get; set; }
        public bool? IsManualControlRecommended { get; set; }
        public List<string> RejectionReasonNames { get; set; }
    }
}