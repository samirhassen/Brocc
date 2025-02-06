using System;
using System.Collections.Generic;
using System.Globalization;
using static NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models.BaseLoanExportRequestModel;

namespace NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models
{
    public class NewOrChangedLoansRequestModel
    {
        public class NewLoanReport
        {
            public TargetEnvironment TargetEnvironment { get; set; }
            public Owner Owner { get; set; }
            public List<Loan> Loans { get; set; }
        }

        public class Borrower
        {
            public IdCodeType IdCodeType { get; set; }
            public string IdCode { get; set; }
        }

        public class ConsumerCredit
        {
            public LoanConsumerProtectionAct LoanConsumerProtectionAct { get; set; }
            public bool IsGoodsOrServicesRelatedCredit { get; set; }
        }

        public class LumpSumLoan
        {
            public decimal AmountIssued { get; set; } 
            public decimal AmountPaid { get; set; } 
            public decimal Balance { get; set; }
            public int AmortizationFrequency { get; set; }
            public LoanPurposeOfUse PurposeOfUse { get; set; }
            public RepaymentMethod RepaymentMethod { get; set; }
        }

        public class Interest
        {
            public decimal TotalInterestRatePct { get; set; }
            public decimal MarginPct { get; set; }
            public InterestType InterestType { get; set; }
            public int InterestDeterminationPeriod { get; set; }
        }

        public class Loan
        {
            public string ReportReference { get; set; }
            public ReportType? ReportType { get; set; } //only for changedloans
            public LoanNumber LoanNumber { get; set; } 
            public bool IsPeerToPeerLoanBroker { get; set; }
            public string LenderMarketingName { get; set; }
            public string ContractDate { get; set; } //string for formatting control
            public CurrencyCode? CurrencyCode { get; set; } //only for newloans
            public decimal OneTimeServiceFees { get; set; } 
            public LoanType LoanType { get; set; } 
            public bool IsTransferredFromAnotherLender { get; set; }
            public int BorrowersCount { get; set; } 
            public List<Borrower> Borrowers { get; set; }
            public ConsumerCredit ConsumerCredit { get; set; }
            public LumpSumLoan LumpSumLoan { get; set; }
            public Interest Interest { get; set; }
            public bool IsLoanWithCollateral { get; set; }
        }

        public enum InterestType
        {
            Euribor,
            BankReferenceRate,
            OtherVariableReferenceRate,
            FixedInterest,
            InterestFree
        }

        public enum LoanPurposeOfUse
        {
            OtherConsumerCredit
        }

        public enum RepaymentMethod
        {
            FixedSizeAmortizations,
            FixedSizePayments,
            Annuities,
            Balloon,
            Bullet,
            Other
        }

        public enum LoanConsumerProtectionAct
        {
            Unknown,
            ConsumerCredit,
            ResidentialPropertyConsumerCredit,
            OtherThanConsumerCredit
        }
    }
}
