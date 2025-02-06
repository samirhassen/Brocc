using NTech.Core.Module.Shared.Database;
using System;

namespace nSavings
{
    public enum DatedSavingsAccountStringCode
    {
        SignedInitialAgreementArchiveKey,
        OcrDepositReference,
        SavingsAccountStatus,
        WithdrawalIban,
        ExternalVariablesKey,
        WelcomeEmailSent
    }

    //For things like annuity and base/margin interest rate that can change over time but where the historical values have impact
    public class DatedSavingsAccountString : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public SavingsAccountHeader SavingsAccount { get; set; }
        public string SavingsAccountNr { get; set; }
        public string Name { get; set; }
        public DateTime TransactionDate { get; set; }
        public BusinessEvent BusinessEvent { get; set; }
        public int BusinessEventId { get; set; }
        public string Value { get; set; }
    }
}