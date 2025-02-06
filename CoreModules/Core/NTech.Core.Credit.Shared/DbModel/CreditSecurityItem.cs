using NTech.Core.Module.Shared.Database;
using System;

namespace nCredit
{
    public class CreditSecurityItem : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string CreditNr { get; set; }
        public CreditHeader Credit { get; set; }
        public string Name { get; set; }
        public string StringValue { get; set; }
        public decimal? NumericValue { get; set; }
        public DateTime? DateValue { get; set; }
        public int CreatedByBusinessEventId { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
    }
}