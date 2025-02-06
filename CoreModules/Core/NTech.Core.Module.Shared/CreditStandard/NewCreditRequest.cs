using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCredit.DbModel.BusinessEvents.NewCredit
{
    public class NewCreditRequestExceptCapital
    {
        public decimal? AnnuityAmount { get; set; }
        public decimal? FixedMonthlyCapitalAmount { get; set; }
        public int? SinglePaymentLoanRepaymentTimeInDays { get; set; }
        public int? RepaymentTimeInMonths { get; set; }
        public string CreditNr { get; set; }
        public string ProviderName { get; set; }
        public int NrOfApplicants { get; set; }
        public bool? IsCompanyCredit { get; set; }
        public decimal? NotificationFee { get; set; }
        public decimal MarginInterestRatePercent { get; set; }
        public string ProviderApplicationId { get; set; } //Optional
        public string ApplicationNr { get; set; } //Optional 
        public string CampaignCode { get; set; } //Optional
        public string SourceChannel { get; set; } //Optional
        public List<Applicant> Applicants { get; set; }
        public List<int> CompanyLoanCollateralCustomerIds { get; set; }
        public List<int> CompanyLoanApplicantCustomerIds { get; set; } //Applicants is the company in this case
        public List<int> CompanyLoanBeneficialOwnerCustomerIds { get; set; }
        public List<int> CompanyLoanAuthorizedSignatoryCustomerIds { get; set; }
        public List<string> ApplicationFreeformDocumentArchiveKeys { get; set; }
        public string SharedAgreementPdfArchiveKey { get; set; } //If not connected to a particular applicant
        public string SniKodSe { get; set; }
        public string BeforeImportCreditNr { get; set; }
        public class Applicant
        {
            public int ApplicantNr { get; set; }
            public int CustomerId { get; set; }
            public string AgreementPdfArchiveKey { get; set; }
        }
        public decimal? ApplicationLossGivenDefault { get; set; }
        public decimal? ApplicationProbabilityOfDefault { get; set; }
    }

    public class NewCreditRequest : NewCreditRequestExceptCapital
    {
        public decimal? CapitalizedInitialFeeAmount { get; set; }
        public decimal? DrawnFromLoanAmountInitialFeeAmount { get; set; }
        public bool? IsInitialPaymentAlreadyMade { get; set; }
        public string Iban { get; set; }
        public string BankAccountNr { get; set; }
        public string BankAccountNrType { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal GetComputedCreditAmount()
        {
            if (HasCreditAmountParts)
            {
                if (CreditAmount > 0)
                    throw new Exception("CreditAmount and CreditAmountParts cannot be combined");
                return CreditAmountParts.Sum(x => x.Amount);
            }
            else
                return CreditAmount;
        }

        public decimal GetDrawnFromLoanAmountInitialFeeAmount()
        {
            if (CreditAmountParts != null)
            {
                if (DrawnFromLoanAmountInitialFeeAmount.HasValue)
                    throw new Exception("DrawnFromLoanAmountInitialFeeAmount and CreditAmountParts cannot be combined");
                return CreditAmountParts.Where(x => x.IsCoveringInitialFeeDrawnFromLoan ?? false).Sum(x => x.Amount);
            }
            else
                return DrawnFromLoanAmountInitialFeeAmount ?? 0m;
        }

        public bool HasCreditAmountParts => CreditAmountParts != null && CreditAmountParts.Count > 0;
        public List<CreditAmountPartModel> CreditAmountParts { get; set; } //Cannot be combined with CreditAmount
        public class CreditAmountPartModel
        {
            public decimal Amount { get; set; }
            public string SubAccountCode { get; set; }
            public bool ShouldBePaidOut { get; set; }
            public bool? IsDirectToCustomerPayment { get; set; }
            public bool? IsSettlingOtherLoan { get; set; }
            public bool? IsCoveringInitialFeeDrawnFromLoan { get; set; }
            public string PaymentBankAccountNr { get; set; }
            public string PaymentBankAccountNrType { get; set; }
            public string PaymentReference { get; set; }
            public string PaymentMessage { get; set; }
        }
        public DirectDebitDetailsModel DirectDebitDetails { get; set; }
        public List<FirstNotificationCostItem> FirstNotificationCosts { get; set; }
        public class FirstNotificationCostItem
        {
            [Required]
            public string CostCode { get; set; }
            [Required]
            public decimal CostAmount { get; set; }
        }

        public class DirectDebitDetailsModel
        {
            public bool? IsActive { get; set; }
            public string AccountNr { get; set; }
            public int? AccountOwner { get; set; }
            public bool? IsExternalStatusActive { get; set; }
            public string DirectDebitConsentFileArchiveKey { get; set; }
        }
    }

    public class NewAdditionalLoanRequest
    {
        public decimal? AdditionalLoanAmount { get; set; }
        public decimal? NewAnnuityAmount { get; set; }
        public decimal? NewMarginInterestRatePercent { get; set; }
        public decimal? NewNotificationFeeAmount { get; set; }
        public string CreditNr { get; set; }
        public string Iban { get; set; }
        public string BankAccountNr { get; set; }
        public string BankAccountNrType { get; set; }
        public string ProviderName { get; set; }
        public string ProviderApplicationId { get; set; } //Optional
        public string ApplicationNr { get; set; } //Optional 
        public string CampaignCode { get; set; } //Optional
        public List<Agreement> Agreements { get; set; }

        public class Agreement
        {
            public int CustomerId { get; set; }
            public string AgreementPdfArchiveKey { get; set; }
        }
    }
}