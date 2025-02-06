using NTech.Core.Module.Shared.Database;

namespace nCredit
{
    public class CreditCustomerListMember : InfrastructureBaseItem
    {
        public string CreditNr { get; set; }
        public CreditHeader Credit { get; set; }
        public int CustomerId { get; set; }
        public string ListName { get; set; }
    }
}