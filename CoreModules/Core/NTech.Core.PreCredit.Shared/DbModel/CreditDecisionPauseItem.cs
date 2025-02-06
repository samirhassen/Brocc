using NTech.Core.Module.Shared.Database;
using System;

namespace nPreCredit
{
    public class CreditDecisionPauseItem : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string RejectionReasonName { get; set; }
        public int CustomerId { get; set; }
        public DateTime PausedUntilDate { get; set; }
        public CreditDecision Decision { get; set; }
        public int CreditDecisionId { get; set; }
    }
}