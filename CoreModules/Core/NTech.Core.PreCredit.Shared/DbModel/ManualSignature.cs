using NTech.Core.Module.Shared.Database;
using System;

namespace nPreCredit
{
    public class ManualSignature : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string SignatureSessionId { get; set; }
        public DateTime CreationDate { get; set; }
        public string CommentText { get; set; }
        public string UnsignedDocumentArchiveKey { get; set; }
        public bool? IsRemoved { get; set; }
        public DateTime? RemovedDate { get; set; }
        public bool? IsHandled { get; set; }
        public DateTime? HandledDate { get; set; }
        public string SignedDocumentArchiveKey { get; set; }
        public DateTime? SignedDate { get; set; }
        public int? HandleByUser { get; set; }
    }
}