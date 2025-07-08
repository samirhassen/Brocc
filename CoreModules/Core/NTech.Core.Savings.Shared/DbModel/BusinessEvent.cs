using System;
using System.Collections.Generic;
using NTech.Core.Module.Shared.Database;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;

namespace NTech.Core.Savings.Shared.DbModel
{
    public enum BusinessEventType
    {
        AccountCreation,
        InterestRateChange,
        AccountCreationRemarkResolution,
        IncomingPaymentFileImport,
        YearlyInterestCapitalization,
        MonthlyInterestCapitalization,
        Withdrawal,
        WithdrawalPenaltyFee,
        AccountClosure,
        PlacementOfUnplacedPayment,
        RepaymentOfUnplacedPayment,
        OutgoingPaymentFileExport,
        WithdrawalAccountChange,
        InitiateWithdrawalAccountChange,
        CancelWithdrawalAccountChange,
        InterestRateChangeRemoval,
        NewManualIncomingPaymentBatch,
        WelcomeEmailSent,
        FixedRateProductAdded,
        FixedRateProductRemoved,
        FixedRateProductChanged,
        FixedRateProductResponse,
        FixedInterestAccountMaturity,
        ManualAccountCapitalization
        //YearlyInterestCapitalization,
        //Withdrawal,
        //OutgoingPaymentFile,
        //Possible
        //future interest debt (for bookkeeping purposes)
        //Customer dies
        //there is some sort of fraud
        //manual correction
        //internal transfers when multiple accounts
        //mispayment - transfer to credit?
        //frozen accounts
    }

    public class BusinessEvent : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string EventType { get; set; }
        public DateTimeOffset EventDate { get; set; }
        public DateTime TransactionDate { get; set; }

        public virtual List<LedgerAccountTransaction> CreatedLedgerTransactions { get; set; }
        public virtual List<SavingsAccountHeader> CreatedSavingsAccounts { get; set; }
        public virtual List<DatedSavingsAccountString> CreatedDatedSavingsAccountStrings { get; set; }
        public virtual List<DatedSavingsAccountValue> CreatedDatedSavingsAccountValues { get; set; }
        public virtual List<SharedDatedValue> CreatedSharedDatedValues { get; set; }
        public virtual List<SavingsAccountCreationRemark> CreatedSavingsAccountCreationRemarks { get; set; }
        public virtual List<SharedSavingsInterestRate> CreatedSharedSavingsInterestRates { get; set; }
        public virtual List<IncomingPaymentFileHeader> CreatedIncomingPaymentFiles { get; set; }

        public virtual List<SavingsAccountInterestCapitalization> CreatedSavingsAccountInterestCapitalizations
        {
            get;
            set;
        }

        public virtual List<OutgoingPaymentHeader> CreatedOutgoingPayments { get; set; }
        public virtual List<OutgoingPaymentFileHeader> CreatedOutgoingPaymentFiles { get; set; }

        public virtual List<SavingsAccountWithdrawalAccountChange> InitiatedSavingsAccountWithdrawalAccountChanges
        {
            get;
            set;
        }

        public virtual List<SavingsAccountWithdrawalAccountChange>
            CommittedOrCancelledSavingsAccountWithdrawalAccountChanges { get; set; }

        public virtual List<SavingsAccountKycQuestion> CreatedSavingsAccountKycQuestions { get; set; }
        public virtual List<SavingsAccountDocument> CreatedSavingsAccountDocuments { get; set; }
        public virtual List<SharedSavingsInterestRate> SharedSavingsInterestRateChangeRemovals { get; set; }
        public virtual List<SharedSavingsInterestRate> SharedSavingsInterestRateAppliesToAccountsSinces { get; set; }

        public virtual List<SharedSavingsInterestRateChangeHeader> CreatedSharedSavingsInterestRateChangeHeaders
        {
            get;
            set;
        }
    }
}