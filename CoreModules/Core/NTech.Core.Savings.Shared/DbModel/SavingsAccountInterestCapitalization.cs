using NTech.Core.Module.Shared.Database;
using System;

namespace nSavings
{
    public class SavingsAccountInterestCapitalization : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public SavingsAccountHeader SavingsAccount { get; set; }
        public string SavingsAccountNr { get; set; }
        public string CalculationDetailsDocumentArchiveKey { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
        public int CreatedByBusinessEventId { get; set; }
    }
}