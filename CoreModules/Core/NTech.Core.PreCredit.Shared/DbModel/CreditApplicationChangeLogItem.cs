using NTech.Core.Module.Shared.Database;

namespace nPreCredit
{
    public class CreditApplicationChangeLogItem : InfrastructureBaseItem
    {
        public enum TransactionTypeCode
        {
            Insert,
            Update,
            Delete
        }

        public int Id { get; set; }
        public string ApplicationNr { get; set; }
        public string Name { get; set; }
        public string GroupName { get; set; }
        public string OldValue { get; set; }
        public string TransactionType { get; set; }
        public CreditApplicationEvent EditEvent { get; set; }
        public int? EditEventId { get; set; }
    }
}