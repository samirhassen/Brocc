using NTech.Core.Module.Shared.Database;
using System;

namespace nSavings
{
    public enum SavingsAccountDocumentTypeCode
    {
        InitialAgreement,
        WithdrawalAccountChangeAgreement
    }
    public class SavingsAccountDocument : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public SavingsAccountHeader SavingsAccount { get; set; }
        public string SavingsAccountNr { get; set; }
        public string DocumentType { get; set; }
        public string DocumentData { get; set; } //Like the year for yearly summaries or similar
        public DateTimeOffset DocumentDate { get; set; }
        public string DocumentArchiveKey { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
        public int CreatedByBusinessEventId { get; set; }
    }
}