using NTech.Core.Module.Shared.Database;
using System.Collections.Generic;

namespace nPreCredit
{
    public class FraudControl : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public CreditApplicationHeader CreditApplication { get; set; }
        public string ApplicationNr { get; set; }
        public int ApplicantNr { get; set; }
        public string Status { get; set; }
        public string RejectionReasons { get; set; }
        public FraudControl ReplacesFraudControl { get; set; }
        public int? ReplacesFraudControl_Id { get; set; }
        public virtual List<FraudControl> ReplacedByFraudControls { get; set; }
        public virtual List<FraudControlItem> FraudControlItems { get; set; }
        public bool IsCurrentData { get; set; }
    }
}