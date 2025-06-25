using System;
using NTech.Core.Module.Shared.Database;

namespace NTech.Core.Savings.Shared.DbModel.SavingsAccountFixed
{
    public class FixedAccountProduct : InfrastructureBaseItem
    {
        public string Id { get; set; }
        
        public string Name { get; set; }
        public decimal InterestRatePercent { get; set; }
        public int TermInMonths { get; set; }

        public DateTime ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }

        public bool? Response { get; set; }
        public DateTime? RespondedAt { get; set; }
        public string RespondedBy { get; set; }
        public BusinessEvent RespondedAtBusinessEvent { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public BusinessEvent CreatedAtBusinessEvent { get; set; }
        
        public string UpdatedBy { get; set; }
        public BusinessEvent UpdatedAtBusinessEvent { get; set; }
    }
}