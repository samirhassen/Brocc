using System.Collections.Generic;
using System.Dynamic;

namespace nPreCredit.Code.Services.CompanyLoans
{
    public class CompanyLoanAgreementPrintContextModel
    {
        public string PrintDate { get; set; }
        public CompanyCustomerModel CompanyCustomer { get; set; }
        public LoanDetailsModel LoanDetails { get; set; }
        public BankAccountModel BankAccount { get; set; }

        public List<CollateralModel> Collaterals { get; set; }
        public bool HasCollaterals { get; set; }
        public List<PercentBeneficialOwnerModel> PercentBeneficialOwners { get; set; }
        public List<ConnectionBeneficialOwnerModel> ConnectionBeneficialOwners { get; set; }
        public List<IsUsPersonBeneficialOwnerModel> UsPersonOwners { get; set; }
        public List<PepPersonModel> PepPersons { get; set; }
        public bool HasPepPersons { get; set; }
        public CashHandlingModel CashHandling { get; set; }
        public CurrencyExchangeModel CurrencyExchange { get; set; }
        public bool HasBeneficialOwners { get; set; }
        public bool HasUsPersonOwners { get; set; }

        public class BankAccountModel
        {
            public string DisplayNr { get; set; }
            public string NormalizedNr { get; set; }
            public string AccountType { get; set; }
            public ExpandoObject AccountTypeFlags { get; set; }
        }

        public class CompanyCustomerModel
        {

            public string Name { get; set; }
            public string Orgnr { get; set; }
            public string StreetAddress { get; set; }
            public string ZipcodeAndCityAddress { get; set; }
            public string CompanySector { get; set; }
            public string CompanyEmployeeCount { get; set; }
            public string PaymentSource { get; internal set; }
            public string CompanyYearlyRevenue { get; internal set; }
            public string LoanPurposeCode { get; internal set; }
            public string IsPaymentServiceProvider { get; internal set; }
            public string ExtraPayments { get; internal set; }
            public string Auditor { get; internal set; }
            public string IsAnyPep { get; internal set; }
        }

        public class LoanDetailsModel
        {
            public string LoanNr { get; set; }
            public string LoanAmount { get; set; }
            public string RepaymentTimeInMonths { get; set; }
            public string AnnuityAmount { get; set; }
            public string MonthlyAmountIncludingFees { get; set; }
            public string MarginInterestRatePercentPerYear { get; set; }
            public string ReferenceInterestRatePercentPerYear { get; set; }
            public string TotalInterestRatePercentPerYear { get; set; }
            public string MarginInterestRatePercentPerMonth { get; set; }
            public string ReferenceInterestRatePercentPerMonth { get; set; }
            public string TotalInterestRatePercentPerMonth { get; set; }
            public string EffectiveInterestRatePercentPerYear { get; set; }
            public string EffectiveInterestRatePercentPerMonth { get; set; }
            public string MonthlyFeeAmount { get; set; }
            public string TotalPaidAmount { get; set; }
            public string InitialFeeAmount { get; set; }
        }

        public class PersonBaseModel
        {
            public string FullName { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string CivicRegNr { get; set; }
        }

        public class CollateralModel : PersonBaseModel
        {

        }

        public class PercentBeneficialOwnerModel : PersonBaseModel
        {
            public string OwnershipPercent { get; set; }

        }

        public class IsUsPersonBeneficialOwnerModel : PersonBaseModel
        {

        }

        public class ConnectionBeneficialOwnerModel : PersonBaseModel
        {
            public string ConnectionText { get; set; }
        }

        public class PepPersonModel : PersonBaseModel
        {
            public string PepRole { get; set; }
        }

        public class CashHandlingModel
        {
            public bool HasCashHandling { get; set; }
            public string Description { get; set; }
            public string YearlyVolume { get; set; }
            public string CompanyRevenue { get; set; }

        }

        public class CurrencyExchangeModel
        {
            public bool HasCurrencyExchange { get; set; }
            public string Description { get; set; }
        }
    }
}