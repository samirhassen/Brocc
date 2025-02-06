using NTech.Core.Module.Shared.Database;

namespace nPreCredit
{
    public class CreditApprovalBatchItemOverride : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string CodeName { get; set; }
        public string ContextData { get; set; }
        public int CreditApprovalBatchItemId { get; set; }
        public CreditApprovalBatchItem BatchItem { get; set; }
    }
}