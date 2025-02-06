using System.Collections.Generic;

namespace nPreCredit.Code.Services
{
    public class UnsecuredLoanStandardAgreementPrintContext
    {
        public List<ApplicantModel> Applicants { get; set; }
        public string HasLoansToSettle { get; set; } //"true" or null. Useful for conditional mustache templates to show/hide headers
        public string IsSwedishHighCostCredit { get; set; }
        public List<LoanToSettleModel> LoanToSettle { get; set; }
        public PersonalSettlementAccountModel PersonalSettlementAccount { get; set; }
        public string CreditNr { get; set; }
        public string LoanAmount { get; set; }
        public string RepaymentTimeInMonths { get; set; }
        public string SinglePaymentLoanRepaymentTimeInDays { get; set; }
        public string AnnuityAmount { get; set; }        
        public string MarginInterestRatePercent { get; set; }
        public string ReferenceInterestRatePercent { get; set; }
        public string TotalInterestRatePercent { get; set; }
        public string NotificationFeeAmount { get; set; }
        public string InitialFeeWithheldAmount { get; set; }
        public string InitialFeeCapitalizedAmount { get; set; }
        public List<FirstNotificationCostModel> FirstNotificationCosts { get; set; }

        public string EnduserContactEmail { get; set; }
        public ProvidedByModel ProvidedBy { get; set; }
        public string TotalPaidAmount { get; set; }
        public string TotalCostAmount { get; set; }
        public string EffectiveInterestRatePercent { get; set; }
        public string ReminderFeeAmount { get; set; }
        public string GeneralTermsRawHtml { get; set; }

        public class FirstNotificationCostModel
        {
            public string CostName { get; set; }
            public string Amount { get; set; }
        }
        public class ApplicantModel
        {
            public string FullName { get; set; }
            public string CivicRegNr { get; set; }
            public string AddressSingleLine { get; set; }
        }

        public class PersonalSettlementAccountModel
        {
            public string AccountTypeName { get; set; }
            public string BankName { get; set; }
            public string AccountNrFormatted { get; set; }
        }

        public class LoanToSettleModel
        {
            public string AccountNrFormatted { get; set; }
            public string AccountTypeName { get; set; }
            public string PaymentReference { get; set; }
            public string PaymentMessage { get; set; }
            public string PaymentReferenceOrMessage { get; set; }
        }

        public class ProvidedByModel
        {
            public string ProviderName { get; set; }
            public string EnduserContactEmail { get; set; }
            public string Address { get; set; }
        }
    }
}