using NTech.Core.Module.Shared.Database;

namespace nPreCredit
{
    public class CreditApplicationSearchTerm : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public CreditApplicationHeader CreditApplication { get; set; }
        public string ApplicationNr { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }
}