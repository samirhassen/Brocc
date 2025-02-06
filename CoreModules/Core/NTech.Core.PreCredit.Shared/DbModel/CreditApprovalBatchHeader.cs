using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nPreCredit
{
    public class CreditApprovalBatchHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public virtual List<CreditApprovalBatchItem> Items { get; set; }
        public int ApprovedById { get; set; }
        public DateTimeOffset ApprovedDate { get; set; }
    }
}