using nCredit.DbModel.BusinessEvents.NewCredit;
using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;

namespace nPreCredit.Code
{
    public interface ICreditClient
    {
        void CreateCredits(NewCreditRequest[] newCreditRequests, NewAdditionalLoanRequest[] additionalLoanRequests);

        decimal GetCurrentReferenceInterest();

        List<HistoricalCredit> GetCustomerCreditHistory(List<int> customerIds);

        List<HistoricalCredit> GetCustomerCreditHistoryByCreditNrs(List<string> creditNrs);

        string NewCreditNumber();

        (string PayerNr, string ClientBankGiroNr) GenerateDirectDebitPayerNumber(string creditNr, int applicantNr);

        void CreateMortgageLoans(MortgageLoanRequest[] mortgageLoanRequests);

        void CreateMortgageLoan(MortgageLoanRequest mortgageLoanRequest);

        List<string> CreateCompanyCredits(CreateCompanyCreditsRequest createCompanyCreditsRequest);

        Tuple<List<string>, List<string>> GenerateReferenceNumbers(int creditNrCount, int ocrNrCount);
    }

    public class MortgageLoanRequest
    {
        public string CreditNr { get; set; }
        public decimal MonthlyFeeAmount { get; set; }
        public decimal NominalInterestRatePercent { get; set; }
        public List<Applicant> Applicants { get; set; }
        public List<Document> Documents { get; set; }
        public int NrOfApplicants { get; set; }
        public string ProviderName { get; set; }
        public string ProviderApplicationId { get; set; }
        public string ApplicationNr { get; set; }
        public DateTime SettlementDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal InterestDifferenceCostAmount { get; set; }
        public bool IsInterestDifferenceCostAmountAddedToFirstNotification { get; set; }
        public Dictionary<string, string> SecurityItems { get; set; }
        public ActiveDirectDebitAccountModel ActiveDirectDebitAccount { get; set; }
        public string KycQuestionsJsonDocumentArchiveKey { get; set; }
        public decimal? CurrentObjectValue { get; set; }

        /// <summary>
        /// The historical date when the current object value is from. If this is not supplied it's assumed to be from today.
        /// </summary>
        public DateTime? CurrentObjectValueDate { get; set; }

        /// <summary>
        /// Initial loan amount.
        /// </summary>
        public decimal? LoanAmount { get; set; }

        /// <summary>
        /// Amortization chosen. Never lower than required but can be higher if the customer wants to pay faster.
        /// </summary>
        public decimal? ActualAmortizationAmount { get; set; }

        /// <summary>
        /// Actual amortization will be 0 until this date is passed then it will fall back to ActualAmortizationAmount
        /// </summary>
        public DateTime? AmortizationExceptionUntilDate { get; set; }

        /// <summary>
        /// Amortization used instead of ActualAmortizationAmount during the time until exception until date
        /// </summary>
        public decimal? ExceptionAmortizationAmount { get; set; }

        /// <summary>
        /// Reasons for exception. Can be one of Nyproduktion, Lantbruksenhet, Sjukdom, Arbetslöshet, Dödsfall
        /// </summary>
        public List<string> AmortizationExceptionReasons { get; set; }

        /// <summary>
        /// When RequiredAmortizationAmount is 0 and the customer want zero we set ActualAmortizationAmount to our default minimum (which is not a regualtory requirement)
        /// and we set this date which will cause the actual amortization to be 0 until this date is passed and then it falls back to our minimum.
        /// </summary>
        public DateTime? AmortizationFreeUntilDate { get; set; }

        /// <summary>
        /// One of the amortization codes in MortageLoanAmortizationRuleCode
        /// </summary>
        public string AmortizationRule { get; set; }

        /// <summary>
        /// The loan amount ysed for amortization calculation. When moving loans will typically be from the other bank.
        /// </summary>
        public decimal? AmortizationBasisLoanAmount { get; set; }

        /// <summary>
        /// The object value used for amortization calculation. When moving loans will typically be from the other bank.
        /// </summary>
        public decimal? AmortizationBasisObjectValue { get; set; }

        /// <summary>
        /// The date of the object value used for amortization calculation. When moving loans will typically be from the other bank.
        /// </summary>
        public DateTime? AmortizationBasisDate { get; set; }

        /// <summary>
        /// Used for calculating loan income ratio for r201723. This + the current loan amount will be used as the loan part of the fraction.
        /// </summary>
        public decimal? DebtIncomeRatioBasisAmount { get; set; }

        /// <summary>
        /// We store the current income since it's used to compute the debt income ratio for r201723
        /// </summary>
        public decimal? CurrentCombinedYearlyIncomeAmount { get; set; }

        /// <summary>
        /// Minimum required amortization amount when using the alternate rule. Cannot be computed since we don't know the initial loan amount
        /// and actual amortization amount can be higher. This is needed when creating an amortization basis for another bank.
        /// </summary>
        public decimal? RequiredAlternateAmortizationAmount { get; set; }

        /// <summary>
        /// Indicates that this loan is a child loan
        /// that is notified and such as part of the main loan instead of on it's own
        /// </summary>
        public string MainCreditCreditNr { get; set; }

        /// <summary>
        /// Additional ocr that is shared with other loans and used to indicate that payments
        /// can be split between this as the client chooses.
        ///
        /// Typical use will be to combine with MainCreditCreditNr and then printing
        /// the SharedPaymentOcrNr on the notifications.
        /// </summary>
        public string SharedOcrPaymentReference { get; set; }

        /// <summary>
        /// Loan is secured by a property so it is a mortgage loan
        /// but the intent of this part of the loan is not to buy a house or
        /// do major repairs so it does not qualify for all the tax benefits of
        /// a full mortgage loan
        /// </summary>
        public bool IsForNonPropertyUse { get; set; }

        /// <summary>
        /// Annuities instead of fixed amortization. Can not be used together with ActualAmortizationAmount.
        /// </summary>
        public decimal? AnnuityAmount { get; set; }

        /// <summary>
        /// Something like 28 meaning the 28th of each month.
        /// </summary>
        public int? NotificationDueDay { get; set; }

        public decimal? ReferenceInterestRate { get; set; }

        public List<AmountModel> LoanAmountParts { get; set; }

        public MortgageLoanCollateralsModel Collaterals { get; set; }

        public class MortgageLoanCollateralsModel
        {
            public List<CollateralModel> Collaterals { get; set; }

            public class CollateralModel
            {
                public bool IsMain { get; set; }
                public string CollateralId { get; set; }
                public List<PropertyModel> Properties { get; set; }
                public List<ValuationModel> Valuations { get; set; }
                public List<int> CustomerIds { get; set; }
            }

            public class PropertyModel
            {
                public string CodeName { get; set; }
                public string DisplayName { get; set; }
                public string TypeCode { get; set; }
                public string CodeValue { get; set; }
                public string DisplayValue { get; set; }
            }

            public class ValuationModel
            {
                public DateTime? ValuationDate { get; set; }
                public decimal Amount { get; set; }
                public string TypeCode { get; set; }
                public string SourceDescription { get; set; }
            }
        }

        public class AmountModel
        {
            public string SubAccountCode { get; set; }
            public decimal Amount { get; set; }
        }

        public class Applicant
        {
            public int ApplicantNr { get; set; }
            public int CustomerId { get; set; }
            public string AgreementPdfArchiveKey { get; set; }
            public decimal? OwnershipPercent { get; set; }
        }

        public class Document
        {
            public string DocumentType { get; set; }
            public int? ApplicantNr { get; set; }
            public string ArchiveKey { get; set; }
        }

        public class ActiveDirectDebitAccountModel
        {
            public int BankAccountNrOwnerApplicantNr { get; set; }
            public string BankAccountNr { get; set; }
            public DateTime ActiveSinceDate { get; set; }
        }
    }

    public class CreateCompanyCreditsRequest
    {
        public List<Credit> Credits { get; set; }

        public class Credit
        {
            public decimal? AnnuityAmount { get; set; }
            public string CreditNr { get; set; }
            public string Iban { get; set; }
            public string BankAccountNr { get; set; }
            public string BankAccountNrType { get; set; }
            public string ProviderName { get; set; }
            public decimal CreditAmount { get; set; }
            public decimal? CapitalizedInitialFeeAmount { get; set; }
            public decimal? DrawnFromLoanAmountInitialFeeAmount { get; set; }
            public decimal? NotificationFee { get; set; }
            public decimal? MarginInterestRatePercent { get; set; }
            public string AgreementPdfArchiveKey { get; set; }
            public int? CompanyCustomerId { get; set; }
            public string ProviderApplicationId { get; set; }
            public string ApplicationNr { get; set; }
            public string CampaignCode { get; set; }
            public string SourceChannel { get; set; }
            public List<int> CompanyLoanCollateralCustomerIds { get; set; }
            public List<int> CompanyLoanApplicantCustomerIds { get; set; }
            public List<int> CompanyLoanBeneficialOwnerCustomerIds { get; set; }
            public List<int> CompanyLoanAuthorizedSignatoryCustomerIds { get; set; }
            public List<string> ApplicationFreeformDocumentArchiveKeys { get; set; }
            public string SniKodSe { get; set; }
        }
    }
}