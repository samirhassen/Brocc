using System;
using NTech.Core.Module.Shared.Database;

namespace NTech.Core.Savings.Shared.DbModel
{
    public class SharedSavingsInterestRateChangeHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public BusinessEvent BusinessEvent { get; set; }
        public int BusinessEventId { get; set; }
        public int InitiatedAndCreatedByUserId { get; set; }
        public DateTimeOffset InitiatedDate { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public int VerifiedByUserId { get; set; }
        public DateTimeOffset VerifiedDate { get; set; }
        public SharedSavingsInterestRate AllAccountsRate { get; set; }
        public int? AllAccountsRateId { get; set; }
        public SharedSavingsInterestRate NewAccountsOnlyRate { get; set; }
        public int? NewAccountsOnlyRateId { get; set; }
    }
}