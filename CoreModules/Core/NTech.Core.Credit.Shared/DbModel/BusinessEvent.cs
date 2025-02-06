using NTech.Core.Credit.Shared.DbModel;
using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nCredit
{
    public enum BusinessEventType
    {
        NewCredit,
        CapitalizedInitialFee,
        ReferenceInterestRateChange,
        NewNotification,
        NewManualIncomingPaymentBatch,
        PlacedUnplacedIncomingPayment,
        Repayment,
        NewOutgoingPaymentFile,
        NewIncomingPaymentFile,
        NewReminder,
        PaymentFreeMonth,
        NewTerminationLetter,
        PostponeTerminationLetters,
        ResumeTerminationLetters,
        AddedPromisedToPayDate,
        RemovedPromisedToPayDate,
        CreditDebtCollectionExport,
        PostponeDebtCollectionExport,
        ResumeDebtCollectionExport,
        NotificationWriteOff,
        AddedFuturePaymentFreeMonth,
        RemovedFuturePaymentFreeMonth,
        NewAdditionalLoan,
        StartedCreditTermsChange,
        AcceptedCreditTermsChange,
        CancelledCreditTermsChange,
        AddedSignedAgreementToCreditTermsChange,
        RemovedSignedAgreementToCreditTermsChange,
        CreditCorrectAndClose,
        StartedCreditSettlementOffer,
        AcceptedCreditSettlementOffer,
        CancelledCreditSettlementOffer,
        Correction, //Used for one off corrections, version migrations and such
        ImportedEInvoiceFiMessageFile,
        StartedEInvoiceFi,
        StoppedEInvoiceFi,
        NewMortgageLoan,
        ChangeDirectDebitStatus,
        ScheduledOutgoingDirectDebitChange,
        NewOutgoingDirectDebitChangeFile,
        ImportedIncomingDirectDebitChangeFile,
        MortgageLoanReferenceInterestUpdate,
        AddedCompanyConnection,
        RemovedCompanyConnection,
        SetApplicationLossGivenDefault,
        SetApplicationProbabilityOfDefault,
        NewCreditHeaderForDirectDebitSettings,
        CreateMortgageLoanCollateral,
        ChangedMortgageLoanFixedInterestRate,
        ChangedMortgageLoanOwner,
        BulkChangedMortgageLoanOwner,
        InactivateTerminationLetter,
        RevalueMortgageLoanSe,
        SetAmortizationExceptionsMortgageLoanSe,
        StartedMlCreditTermsChange,
        ScheduledMlCreditTermsChange,
        MlDefaultCreditTermsChange,
        MlRebindingReminderMessage,
        RemoveCreditCustomer, //TODO: When we finish up testing this function it should most likely not have it's own event but rather be an effect of other events.
        AlternatePaymentPlanCreated,
        AlternatePaymentPlanCancelled,
        AlternatePaymentPlanCompleted,
        AlternatePaymentPlanCancelledManually,
        ScheduledDirectDebitPayment
    }

    public class BusinessEvent : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string EventType { get; set; }
        public DateTimeOffset EventDate { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime BookKeepingDate { get; set; }
        public virtual List<CreditHeader> CreatedCredits { get; set; }
        public virtual List<AccountTransaction> Transactions { get; set; }
        public virtual List<DatedCreditValue> DatedCreditValues { get; set; }
        public virtual List<DatedCreditCustomerValue> DatedCreditCustomerValues { get; set; }
        public virtual List<DatedCreditString> DatedCreditStrings { get; set; }
        public virtual List<DatedCreditDate> DatedCreditDates { get; set; }
        public virtual List<DatedCreditDate> RemovedDatedCreditDates { get; set; }
        public virtual List<SharedDatedValue> SharedDatedValues { get; set; }
        public virtual List<OutgoingPaymentHeader> CreatedOutgoingPayments { get; set; }
        public virtual List<OutgoingPaymentFileHeader> CreatedOutgoingPaymentFiles { get; set; }
        public virtual List<IncomingPaymentFileHeader> CreatedIncomingPaymentFiles { get; set; }
        public virtual List<OutgoingCreditNotificationDeliveryFileHeader> CreatedNotificationDeliveryFiles { get; set; }
        public virtual List<CreditPaymentFreeMonth> CreditPaymentFreeMonths { get; set; }
        public virtual List<CreditFuturePaymentFreeMonth> CreatedCreditFuturePaymentFreeMonths { get; set; }
        public virtual List<CreditFuturePaymentFreeMonth> CancelledCreditFuturePaymentFreeMonths { get; set; }
        public virtual List<CreditFuturePaymentFreeMonth> CommitedCreditFuturePaymentFreeMonths { get; set; }
        public virtual List<CreditTermsChangeHeader> StartedTermsChanges { get; set; }
        public virtual List<CreditTermsChangeHeader> CommitedTermsChanges { get; set; }
        public virtual List<CreditTermsChangeHeader> CancelledTermsChanges { get; set; }
        public virtual List<CreditTermsChangeItem> AddedCreditTermsChangeItems { get; set; }
        public virtual List<CreditSettlementOfferHeader> StartedCreditSettlementOffers { get; set; }
        public virtual List<CreditSettlementOfferHeader> CommitedCreditSettlementOffers { get; set; }
        public virtual List<CreditSettlementOfferHeader> CancelledCreditSettlementOffers { get; set; }
        public virtual List<EInvoiceFiMessageHeader> CreatedEInvoiceFiMessageHeaders { get; set; }
        public virtual List<EInvoiceFiAction> ConnectedEInvoiceFiActions { get; set; }
        public virtual List<CreditSecurityItem> CreatedCreditSecurityItems { get; set; }
        public virtual List<CreditOutgoingDirectDebitItem> CreatedCreditOutgoingDirectDebitItems { get; set; }
        public virtual List<OutgoingDirectDebitStatusChangeFileHeader> CreatedOutgoingDirectDebitStatusChangeFileHeaders { get; set; }
        public virtual List<IncomingDirectDebitStatusChangeFileHeader> CreatedIncomingDirectDebitStatusChangeFileHeaders { get; set; }
        public virtual List<CreditComment> CreatedCreditComments { get; set; }
        public virtual List<ReferenceInterestChangeHeader> CreatedReferenceInterestChangeHeaders { get; set; }
        public virtual List<CollateralHeader> CreatedCollaterals { get; set; }
        public virtual List<CollateralItem> CreatedCollateralItems { get; set; }
        public virtual List<CollateralItem> RemovedCollateralItems { get; set; }
        public virtual List<FixedMortgageLoanInterestRate> CreatedFixedMortgageLoanInterestRates { get; set; }
        public virtual List<HFixedMortgageLoanInterestRate> CreatedHFixedMortgageLoanInterestRates { get; set; }
        public virtual List<CreditTerminationLetterHeader> InactivatedTerminationLetters { get; set; }
        public virtual List<AlternatePaymentPlanHeader> CreatedAlternatePaymentPlanHeaders { get; set; }
        public virtual List<AlternatePaymentPlanHeader> CancelledAlternatePaymentPlanHeaders { get; set; }
        public virtual List<AlternatePaymentPlanHeader> FullyPaidAlternatePaymentPlanHeaders { get; set; }
    }
}