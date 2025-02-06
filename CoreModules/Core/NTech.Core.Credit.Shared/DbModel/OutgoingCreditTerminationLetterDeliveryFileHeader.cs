using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nCredit
{
    public class OutgoingCreditTerminationLetterDeliveryFileHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public DateTime TransactionDate { get; set; }
        public string FileArchiveKey { get; set; }
        public string ExternalId { get; set; }
        public virtual List<CreditTerminationLetterHeader> TerminationLetters { get; set; }
    }
}