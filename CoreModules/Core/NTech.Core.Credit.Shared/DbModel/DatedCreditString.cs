using NTech.Core.Module.Shared.Database;
using System;

namespace nCredit
{
    public enum DatedCreditStringCode
    {
        Iban,
        BankAccountNr,
        BankAccountNrType,
        SignedInitialAgreementArchiveKey,
        OcrPaymentReference,
        CreditStatus,
        NextInterestFromDate,
        ProviderApplicationId,
        ApplicationNr,
        DebtCollectionFileExternalId,
        EInvoiceFiStatus,
        EInvoiceAddress,
        EInvoiceFiBankCode,
        AmortizationModel,
        IsDirectDebitActive,
        DirectDebitAccountOwnerApplicantNr,
        DirectDebitBankAccountNr,
        IntialLoanCampaignCode,
        
        /// <summary>
        /// Reasons for exception. For mortgage loan constrained to one of: Nyproduktion, Lantbruksenhet, Sjukdom, Arbetslöshet, Dödsfall
        /// </summary>
        AmortizationExceptionReasons,
        /// <summary>
        /// Similar to an affiliate/provider this can be for instance a campaign or an ad partner.
        /// </summary>
        InitialLoanSourceChannel,
        KycQuestionsJsonDocumentArchiveKey,
        CompanyLoanSniKodSe,
        /// <summary>
        /// Indicates that this loan is a child loan
        /// that is notified and such as part of the main loan instead of on it's own.
        /// </summary>
        MainCreditCreditNr,

        /// <summary>
        /// Loan is secured by a property so it is a mortgage loan
        /// but the intent of this part of the loan is not to buy a house or
        /// do major repairs so it does not qualify for all the tax benefits of
        /// a full mortgage loan
        /// </summary>
        IsForNonPropertyUse,

        /// <summary>
        /// Additional ocr that can be used to indicate a payment can be split across a group of credits
        /// as the client wishes.
        /// </summary>
        SharedOcrPaymentReference,

        /// <summary>
        /// For credits imported from other systems this tracks their previous nr
        /// </summary>
        BeforeImportCreditNr,

        /// <summary>
        /// Loan owner management for loans. Used for tagging mortgage loans with which obligation owner they belong to.
        /// </summary>
        LoanOwner,

        /// <summary>
        /// A group of loan with the same agreement nr are supposed to:
        /// - have a single signed agreement which this refers to that allows payments to be distributed between the loans
        /// - the loans will be co notified (a common shared payment ocr will be generated)
        /// - basically this concept tries to maintain  the illusion that there is just "one loan" from the consumers perspective
        ///   where the actual loans are more like loan parts.
        /// </summary>
        MortgageLoanAgreementNr,
        /// <summary>
        /// Stops the standard default process (kravkedja).
        /// Used for instance for estate management and when an alternate payment plan is in effect.
        /// </summary>
        IsStandardDefaultProcessSuspended
    }

    //For things like annuity and base/margin interest rate that can change over time but where the historical values have impact
    public class DatedCreditString : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public CreditHeader Credit { get; set; }
        public string CreditNr { get; set; }
        public string Name { get; set; }
        public DateTime TransactionDate { get; set; }
        public BusinessEvent BusinessEvent { get; set; }
        public int BusinessEventId { get; set; }
        public string Value { get; set; }
    }
}