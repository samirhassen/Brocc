using NTech.Core.Module.Shared.Database;
using System;

namespace nPreCredit
{
    public class CreditApplicationCancellation : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public CreditApplicationHeader CreditApplication { get; set; }
        public DateTimeOffset CancelledDate { get; set; }
        public string CancelledState { get; set; }
        public string ApplicationNr { get; set; }
        public bool WasAutomated { get; set; }
        public int CancelledBy { get; set; }
    }
}