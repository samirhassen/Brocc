using System;

namespace nPreCredit
{
    public class PauseItemScoringModel
    {
        public string ApplicationNr { get; set; }
        public string RejectionReasonName { get; set; }
        public int CustomerId { get; set; }
        public DateTime PausedUntilDate { get; set; }
    }
}
