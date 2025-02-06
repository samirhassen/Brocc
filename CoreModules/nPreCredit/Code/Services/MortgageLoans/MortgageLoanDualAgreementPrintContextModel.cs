using System.Collections.Generic;
//NOTE: This maps to how mustache templates see the world so we don't want c# conventions here, hence disabling checks.
// ReSharper disable All

namespace nPreCredit.Code.Services.MortgageLoans
{
    public class MortgageLoanDualAgreementPrintContextModel
    {
        public List<ApplicantPersonModel> applicantperson { get; set; }
        public List<CollateralAgreementModel> othercollateral_agreement { get; set; }
        public List<CollateralInfoModel> othercollateral_info { get; set; }
        public GeneralTermsModel generalterms { get; set; }
        public DebugModel debug_data { get; set; }

        public class DebugModel
        {
            public List<DebugItem> debug_items { get; set; }
        }

        public class DebugItem
        {
            public string header { get; set; }
            public string label { get; set; }
            public string text { get; set; }
        }

        public class DocumentModel
        {
            public string TestContext { get; set; }
        }

        public class GeneralTermsModel : DocumentModel
        {
        }

        public class ApplicantPersonModel
        {
            public MortgageLoanAgreementModel mlagreement { get; set; }
            public MortgageLoanAgreementModel ulagreement { get; set; }
            public EsisModel esis { get; set; }
            public SekkiModel sekki { get; set; }
            public CollateralAgreementModel maincollateral { get; set; }
            public List<CollateralInfoModel> othercollateral_info_copies { get; set; }
        }

        public class CollateralInfoModel : DocumentModel
        {
            public ContactModel applicant { get; internal set; }
            public ContactModel collateralowner { get; internal set; }
            public string employment { get; internal set; }
            public string employedSince { get; internal set; }
            public string employer { get; internal set; }
            public string profession { get; internal set; }
            public string employedTo { get; internal set; }
            public string marriage { get; internal set; }
            public string monthlyIncomeSalaryAmount { get; internal set; }
            public string monthlyIncomePensionAmount { get; internal set; }
            public string monthlyIncomeCapitalAmount { get; internal set; }
            public string monthlyIncomeBenefitsAmount { get; internal set; }
            public string monthlyIncomeOtherAmount { get; internal set; }
            public string childrenMinorCount { get; internal set; }
            public string childrenAdultCount { get; internal set; }
            public string costOfLivingRent { get; internal set; }
            public string costOfLivingFees { get; internal set; }
        }

        public class CollateralPersonModel
        {
            public CollateralAgreementModel othercollateral_agreement { get; set; }
            public CollateralInfoModel othercollateral_info { get; set; }
        }

        public class ContactModel
        {
            public string civicRegNr { get; set; }
            public string fullName { get; set; }
            public string streetAddress { get; set; }
            public string areaAndZipcode { get; set; }
            public string email { get; set; }
            public string phone { get; set; }
        }

        public class MortgageLoanAgreementModel : DocumentModel
        {
            public string printDate { get; set; }
            public ContactModel contact1 { get; set; }
            public ContactModel contact2 { get; set; }
            public ContactModel contact_current { get; set; }
            public string loanNumber { get; set; }
            public string loanAmount { get; set; }
            public string loanAmountIncludingCapitalizedInitialFees { get; set; }
            public string is_ml { get; set; }
            public string is_ul { get; set; }
            public string marginInterestRate { get; internal set; }
            public string referenceInterestRate { get; internal set; }
            public string totalInterestRate { get; internal set; }
            public string repaymentTimeInMonths { get; internal set; }
            public string notificationFeeAmount { get; internal set; }
            public string totalInitialFeeAmount { get; internal set; }
            public string monthlyPaymentIncludingFees { get; set; }
            public string monthlyPaymentExcludingFees { get; set; }
            public string effectiveInterestRate { get; internal set; }
            public string notificationDueDay { get; internal set; }
            public string projectedEndDate { get; internal set; }
            public string totalPaidAmount { get; internal set; }
            public string projectedFirstDueDate { get; internal set; }
            public Dictionary<string, string> fee { get; set; }
            public BankAccountNrModel consumerDirectPaymentBankAccount { get; set; }
            public string hasDirectPaymentToCustomer { get; set; }
        }

        public class BankAccountNrModel
        {
            public string raw { get; set; }
            public string normalized { get; set; }
            public string displayFormatted { get; set; }
        }

        public class EsisModel : DocumentModel
        {
            public string loanAmount { get; set; }
            public string loanAmountIncludingCapitalizedInitialFees { get; set; }
            public string repaymentTimeInMonths { get; set; }
            public string totalPaidAmount { get; set; }
            public string printDate { get; internal set; }
            public ContactModel contact_current { get; internal set; }
            public string paidAmountPerLoanCurrencyUnit { get; internal set; }
            public string paidAmountPerLoanCurrencyUnitIncludingCapitalizedInitialFees { get; set; }
            public string effectiveInterestRate { get; internal set; }
            public string marginInterestRate { get; internal set; }
            public string referenceInterestRate { get; internal set; }
            public string totalInterestRate { get; internal set; }
            public string totalInterestAmount { get; internal set; }
            public string totalInitialFeeAmount { get; internal set; }
            public string totalNotificationFeeAmount { get; internal set; }
            public string monthlyPaymentExcludingFees { get; internal set; }
            public string monthlyPaymentIncludingFees { get; internal set; }
            public string dueDay { get; internal set; }
            public string projectedFirstDueDate { get; internal set; }
            public string projectedLastDate { get; internal set; }
            public string stressedEffectiveInterestRate { get; internal set; }
            public string stressedTotalInterestRate { get; internal set; }
            public string stressedReferenceInterestRate { get; internal set; }
            public string stressedMonthlyPaymentExcludingFees { get; internal set; }
            public string stressedMonthlyPaymentIncludingFees { get; internal set; }
        }

        public class SekkiModel : DocumentModel
        {
            public string loanAmount { get; set; }
            public string loanAmountIncludingCapitalizedInitialFees { get; set; }
            public string totalCostAmount { get; set; }
            public string totalPaidAmount { get; set; }
            public string repaymentTimeInMonths { get; internal set; }
            public string monthlyPaymentExcludingFees { get; internal set; }
            public string monthlyPaymentIncludingFees { get; internal set; }
            public string totalNotificationFeeAmount { get; internal set; }
            public string totalInterestAmount { get; internal set; }
            public string marginInterestRate { get; internal set; }
            public string referenceInterestRate { get; internal set; }
            public string totalInterestRate { get; internal set; }
            public string effectiveInterestRate { get; internal set; }
            public string notificationFeeAmount { get; internal set; }
            public string totalInitialFeeAmount { get; internal set; }
        }

        public class CollateralAgreementModel : DocumentModel
        {
            public List<ContactModel> collateralowner { get; internal set; }
            public HousingCompanyModel property_housing_company { get; internal set; }
            public EstateModel property_estate { get; internal set; }
            public List<CollateralLoanModel> collateral_loans { get; set; }
            public string printDate { get; internal set; }
            public List<ContactModel> loandebtor { get; internal set; }
            public string addressStreet { get; internal set; }
            public string addressCity { get; internal set; }
            public string addressZipCode { get; internal set; }

            public class HousingCompanyModel
            {
                public string housingCompanyName { get; internal set; }
                public string housingCompanyShareCount { get; internal set; }
            }

            public class EstateModel
            {
                public string estatePropertyId { get; internal set; }
                public List<EstateDeedModel> estate_deeds { get; internal set; }
            }

            public class EstateDeedModel
            {
                public string deedNr { get; internal set; }
                public string deedAmount { get; internal set; }
            }

            public class CollateralLoanModel
            {
                public string loanNr { get; set; }
                public string loanAmount { get; set; }
                public string loanAmountIncludingCapitalizedInitialFees { get; set; }
            }
        }

        //Can be observed during generation by externals to not have to look this data up multiple times in multiple ways
        public class SideChannelData
        {
            public int? CreditDecsionId { get; set; }
            public List<LoanModel> Loans { get; set; }
            public Dictionary<int, MortgageLoanDualAgreementService.CustomerModel> Customers { get; set; }

            public class LoanModel
            {
                public bool IsMainLoan { get; set; }
                public decimal LoanAmount { get; set; }
            }
        }
    }
}