using NTech.Core.Module.Shared.Database;

namespace nPreCredit
{
    public class CreditApplicationItem : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public CreditApplicationHeader CreditApplication { get; set; }
        public string ApplicationNr { get; set; }
        public string GroupName { get; set; }
        public string Name { get; set; }
        public bool IsEncrypted { get; set; }
        public string AddedInStepName { get; set; }
        public string Value { get; set; }
    }
}