using NTech.Core.Module.Shared.Database;
using System;

namespace nCredit
{
    public class IncomingDirectDebitStatusChangeFileHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public DateTime TransactionDate { get; set; }
        public string FileArchiveKey { get; set; }
        public string Filename { get; set; } //Duplicated here to allow duplicate checks on import since bgc dont use ids in their files
        public BusinessEvent CreatedByEvent { get; set; }
        public int CreatedByEventId { get; set; }
    }
}