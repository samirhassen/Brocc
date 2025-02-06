using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nCredit
{
    public class EInvoiceFiMessageHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string ExternalMessageType { get; set; }
        public string ExternalMessageId { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
        public int CreatedByEventId { get; set; }
        public DateTimeOffset ExternalTimestamp { get; set; }
        public DateTime ImportDate { get; set; }
        public int ImportedByUserId { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public int? ProcessedByUserId { get; set; }
        public virtual IList<EInvoiceFiMessageItem> Items { get; set; }
        public virtual IList<EInvoiceFiAction> Actions { get; set; }
    }

    public interface IEInvoiceFiMessageHeader
    {
        string GetItemValue(EInvoiceFiItemCode itemCode);
        string ExternalMessageType { get; }
        string ExternalMessageId { get; }
        DateTime ImportDate { get; }
    }
}