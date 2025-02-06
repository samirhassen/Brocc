using nCredit.DomainModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nCredit.DbModel.BusinessEvents
{

    public class MortgageLoanRequest
    {
        public string CreditNr { get; set; }

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
        
        public string MortgageLoanAgreementNr { get; set; }

        /// <summary>
        /// Loan is secured by a property so it is a mortgage loan
        /// but the intent of this part of the loan is not to buy a house or
        /// do major repairs so it does not qualify for all the tax benefits of
        /// a full mortgage loan
        /// </summary>
        public bool IsForNonPropertyUse { get; set; }

        /// <summary>
        /// Something like 28 meaning the 28th of each month.
        /// </summary>
        public int? NotificationDueDay { get; set; }

        public decimal MonthlyFeeAmount { get; set; }

        public decimal NominalInterestRatePercent { get; set; }

        public List<Applicant> Applicants { get; set; }

        public List<Document> Documents { get; set; }

        public int NrOfApplicants { get; set; }

        public string ProviderName { get; set; }
        public string ProviderApplicationId { get; set; }
        public string ApplicationNr { get; set; }

        /// <summary>
        /// Capital and initial interest rate are backdated to this date
        /// to enable the first notification to include interest from before the loan was added to the system.
        /// Cannot be forward in time.
        /// If this is not included they are dated to 'today'
        /// </summary>
        public DateTime? HistoricalStartDate { get; set; }

        public DateTime SettlementDate { get; set; }
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Interest rate will be updated from reference interest and rebound at this date.
        /// If not included it will default to today.
        /// </summary>
        public DateTime? NextInterestRebindDate { get; set; }

        /// <summary>
        /// Required when NextInterestRebindDate is used. Indicates how long the current binding period is.
        /// </summary>
        public int? InterestRebindMounthCount { get; set; }

        /// <summary>
        /// Initial reference interest rate.
        ///
        /// If not included it will default to the current system value or 0 if none is present.
        /// </summary>
        public decimal? ReferenceInterestRate { get; set; }

        public List<AmountModel> DrawnFromLoanAmountInitialFees { get; set; }
        public List<AmountModel> CapitalizedInitialFees { get; set; }

        public class AmountModel
        {
            public string SubAccountCode { get; set; }
            public decimal Amount { get; set; }
        }

        public MortgageLoanCollateralsModel Collaterals { get; set; }

        public ActiveDirectDebitAccountModel ActiveDirectDebitAccount { get; set; }

        /// <summary>
        /// Initial loan amount.
        /// </summary>
        public decimal? LoanAmount { get; set; }

        //Or LoanAmount. Cant be combined
        public List<AmountModel> LoanAmountParts { get; set; }

        /// <summary>
        /// Amortization chosen. Never lower than required but can be higher if the customer wants to pay faster.
        /// </summary>
        public decimal? ActualAmortizationAmount { get; set; }

        /// <summary>
        /// Annuities instead of fixed amortization. Can not be used together with ActualAmortizationAmount.
        /// </summary>
        public decimal? AnnuityAmount { get; set; }

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
        /// Links to the json document containing the customer and product questions asked during the application.
        ///
        /// Example structure (not all questions)
        /// {
        ///    "AnswerDate": "2021-05-31T06:00:00+02:00",
        ///    "Items": [
        ///        {
        ///            "ApplicantNr": 1,
        ///            "IsCustomerQuestion": true,
        ///            "QuestionGroup": "customer",
        ///            "QuestionCode": "citizenCountries",
        ///            "AnswerCode": "SE,DK",
        ///            "QuestionText": "Vilka är dina medborgarskap?",
        ///            "AnswerText": "Sverige,Danmark"
        ///        },
        ///        {
        ///            "ApplicantNr": 1,
        ///            "IsCustomerQuestion": true,
        ///            "QuestionGroup": "customer",
        ///            "QuestionCode": "isPep",
        ///            "AnswerCode": "yes",
        ///            "QuestionText": "Är du, någon i din familj, eller närstående, en politiskt exponerad person?",
        ///            "AnswerText": "Ja"
        ///        },
        ///        {
        ///            "ApplicantNr": 1,
        ///            "IsCustomerQuestion": true,
        ///            "QuestionGroup": "customer",
        ///            "QuestionCode": "pepWho",
        ///            "AnswerCode": "memberofparliament,supremecourtjudge",
        ///            "QuestionText": "Ange om du, någon i din familj, eller närstående, har eller har haft någon av följande roller?",
        ///            "AnswerText": "Parlamentsledamot.,Domare i högsta domstolen, konstitutionell domstol eller liknande befattning."
        ///        },
        ///        {
        ///            "ApplicantNr": null,
        ///            "IsCustomerQuestion": false,
        ///            "QuestionGroup": "product",
        ///            "QuestionCode": "paymentSource",
        ///            "AnswerCode": "salary",
        ///            "QuestionText": "Var kommer pengarna till räntebetalningar och amorteringar huvudsakligen ifrån?",
        ///            "AnswerText": "Lön"
        ///        }
        ///    ]
        /// }
        /// </summary>
        public string KycQuestionsJsonDocumentArchiveKey { get; set; }

        public List<FirstNotificationCostItem> FirstNotificationCosts { get; set; }
        public class FirstNotificationCostItem
        {
            /// <summary>
            /// Cost code. Code must be registred first.
            /// </summary>
            [Required]
            public string CostCode { get; set; }

            /// <summary>
            /// Cost amount
            /// </summary>
            [Required]
            public decimal CostAmount { get; set; }
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

        public List<int> ConsentingPartyCustomerIds { get; set; }
        public List<int> PropertyOwnerCustomerIds { get; set; }

        public int? CollateralId { get; set; }
        public string LoanOwnerName { get; set; }
    }

}