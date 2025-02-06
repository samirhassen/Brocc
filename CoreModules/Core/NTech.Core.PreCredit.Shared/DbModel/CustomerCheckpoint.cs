using NTech.Core.Module.Shared.Database;
using System;

namespace nPreCredit
{
    public class CustomerCheckpoint : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public bool IsCurrentState { get; set; }
        public string ReasonText { get; set; }
        public bool IsReasonTextEncrypted { get; set; }
        public bool IsCheckpointActive { get; set; }
        public DateTime StateDate { get; set; }
        public int StateBy { get; set; }
    }
}