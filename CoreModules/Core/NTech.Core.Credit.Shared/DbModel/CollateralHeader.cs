using NTech.Core.Module.Shared.Database;
using System.Collections.Generic;

namespace nCredit
{
    public class CollateralHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string CollateralType { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
        public int CreatedByBusinessEventId { get; set; }
        public virtual List<CreditHeader> Credits { get; set; }
        public virtual List<CollateralItem> Items { get; set; }
    }
}