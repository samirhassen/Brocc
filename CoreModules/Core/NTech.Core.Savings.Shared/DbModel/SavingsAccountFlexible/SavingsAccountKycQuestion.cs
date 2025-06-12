using NTech.Core.Module.Shared.Database;

namespace NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible
{
    public enum SavingsAccountKycQuestionGroupCode
    {
        Product,
        Customer
    }
    public class SavingsAccountKycQuestion : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public SavingsAccountHeader SavingsAccount { get; set; }
        public string SavingsAccountNr { get; set; }
        public string Name { get; set; }
        public string Group { get; set; }
        public string Value { get; set; }
        public BusinessEvent BusinessEvent { get; set; }
        public int BusinessEventId { get; set; }
    }
}