using System;
using System.Collections.Generic;

namespace nCreditReport
{
    public class CreditReportHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public string CreditReportProviderName { get; set; }
        public DateTimeOffset RequestDate { get; set; }
        public DateTimeOffset? TryArchiveAfterDate { get; set; }
        public string EncryptionKeyName { get; set; }
        public virtual List<CreditReportSearchTerm> SearchTerms { get; set; }
        public virtual List<EncryptedCreditReportItem> EncryptedItems { get; set; }
    }
}