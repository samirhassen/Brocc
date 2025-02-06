using System;
using System.Collections.Generic;

namespace NTech.Banking.ScoringEngine
{
    public class HistoricalApplication
    {
        public string ApplicationNr { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset? ArchivedDate { get; set; }
        public string CreditCheckStatus { get; set; }
        public string CreditNr { get; set; }
        public DateTimeOffset ApplicationDate { get; set; }
        public bool IsFinalDecisionMade { get; set; }
        public IList<HistoricalApplicationPauseItem> PauseItems { get; set; }
        public IList<string> RejectionReasonSearchTerms { get; set; }
        public bool IsMortgageLoanApplication { get; set; }
        public string ApplicationType { get; set; }
    }

    public class HistoricalApplicationPauseItem
    {
        public string RejectionReasonName { get; set; }
        public int CustomerId { get; set; }
        public DateTime PausedUntilDate { get; set; }
    }
}
