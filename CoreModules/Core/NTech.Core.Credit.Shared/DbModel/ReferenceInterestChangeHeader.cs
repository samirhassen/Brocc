using NTech.Core.Module.Shared.Database;
using System;

namespace nCredit
{
    public class ReferenceInterestChangeHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal NewInterestRatePercent { get; set; }
        public int InitiatedByUserId { get; set; }
        public DateTime InitiatedDate { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
        public int CreatedByBusinessEventId { get; set; }
    }
}