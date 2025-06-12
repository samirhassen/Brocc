using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using NTech.Core.Module.Shared.Database;

namespace NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible
{
    public enum SavingsAccountStatusCode
    {
        FrozenBeforeActive,
        Active,
        Closed
    }

    public enum SavingsAccountTypeCode
    {
        StandardAccount,
        FixedInterestAccount
    }

    public class SavingsAccountHeader : InfrastructureBaseItem
    {
        public string SavingsAccountNr { get; set; }
        public int MainCustomerId { get; set; }
        public string AccountTypeCode { get; set; }
        public string Status { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
        public int CreatedByBusinessEventId { get; set; }

        public DateTime? MaturesAt { get; set; }
        public string FixedInterestProduct { get; set; }

        public virtual List<LedgerAccountTransaction> Transactions { get; set; }
        public virtual List<DatedSavingsAccountValue> DatedValues { get; set; }
        public virtual List<DatedSavingsAccountString> DatedStrings { get; set; }
        public virtual List<SavingsAccountComment> Comments { get; set; }
        public virtual List<SavingsAccountCreationRemark> CreationRemarks { get; set; }
        public virtual List<SavingsAccountInterestCapitalization> SavingsAccountInterestCapitalizations { get; set; }
        public virtual List<SavingsAccountWithdrawalAccountChange> SavingsAccountWithdrawalAccountChanges { get; set; }
        public virtual List<SavingsAccountKycQuestion> SavingsAccountKycQuestions { get; set; }
        public virtual List<SavingsAccountDocument> Documents { get; set; }
    }
}