using NTech.Core.Module.Shared.Database;

namespace nCredit
{
    public class CreditCustomer : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string CreditNr { get; set; }
        public int ApplicantNr { get; set; }
        public int CustomerId { get; set; }
        public CreditHeader Credit { get; set; }
    }
}