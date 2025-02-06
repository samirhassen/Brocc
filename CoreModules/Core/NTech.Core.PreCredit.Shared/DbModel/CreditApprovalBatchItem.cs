using NTech.Core.Module.Shared.Database;
using System.Collections.Generic;

namespace nPreCredit
{
    public class CreditApprovalBatchItem : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string ApplicationNr { get; set; }
        public CreditApplicationHeader Application { get; set; }
        public string CreditNr { get; set; }
        public string ApprovalType { get; set; }
        public decimal ApprovedAmount { get; set; }
        public int DecisionById { get; set; }
        public int ApprovedById { get; set; }
        public int CreditApprovalBatchHeaderId { get; set; }
        public CreditApprovalBatchHeader CreditApprovalBatch { get; set; }
        public virtual List<CreditApprovalBatchItemOverride> Overrides { get; set; }
    }
}