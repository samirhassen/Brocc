using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nCustomer.DbModel
{
    public class CustomerCheckpoint : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public bool IsCurrentState { get; set; }
        public string ReasonText { get; set; }
        public DateTime StateDate { get; set; }
        public int StateBy { get; set; }
        public virtual List<CustomerCheckpointCode> Codes { get; set; }
    }
}