using NTech.Core.Module.Shared.Database;
using System;

namespace nPreCredit
{
    public class CreditApplicationPauseItem : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string PauseReasonName { get; set; }
        public int CustomerId { get; set; }
        public DateTime PausedUntilDate { get; set; }
        public CreditApplicationHeader CreditApplication { get; set; }
        public string ApplicationNr { get; set; }
        public int? RemovedBy { get; set; }
        public DateTimeOffset? RemovedDate { get; set; }
    }
}