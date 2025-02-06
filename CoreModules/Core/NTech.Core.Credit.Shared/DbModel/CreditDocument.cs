using NTech.Core.Module.Shared.Database;

namespace nCredit
{
    public class CreditDocument : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public CreditReminderHeader Reminder { get; set; }
        public int? CreditReminderHeaderId { get; set; }
        public CreditTerminationLetterHeader TerminationLetter { get; set; }
        public int? CreditTerminationLetterHeaderId { get; set; }
        public string CreditNr { get; set; }
        public CreditHeader Credit { get; set; }
        public string ArchiveKey { get; set; }
        public string DocumentType { get; set; }
        public int? ApplicantNr { get; set; }
        public int? CustomerId { get; set; }
    }
}