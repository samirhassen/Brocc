using System;
using System.Collections.Generic;

namespace nPreCredit.Code.Services
{
    public class ApplicationInfoModel
    {
        public string ApplicationNr { get; set; }
        public string ApplicationType { get; set; }
        public int NrOfApplicants { get; set; }
        public string CreditCheckStatus { get; set; }
        public string FraudCheckStatus { get; set; }
        public string CustomerCheckStatus { get; set; }
        public string AgreementStatus { get; set; }
        public string MortgageLoanDocumentCheckStatus { get; set; }
        public bool IsActive { get; set; }
        public bool IsWaitingForAdditionalInformation { get; set; }
        public bool IsFinalDecisionMade { get; set; }
        public DateTimeOffset? FinalDecisionDate { get; set; }
        public bool IsCancelled { get; set; }
        public bool IsRejected { get; set; }
        public string ProviderName { get; set; }
        public string ProviderDisplayName { get; set; }
        public DateTimeOffset ApplicationDate { get; set; }
        public bool IsPartiallyApproved { get; set; }
        public bool IsRejectAllowed { get; set; }
        public bool IsApproveAllowed { get; set; }
        public bool IsMortgageLoanApplication { get; set; }
        public string MortgageLoanAdditionalQuestionsStatus { get; set; }
        public string MortgageLoanInitialCreditCheckStatus { get; set; }
        public string MortgageLoanFinalCreditCheckStatus { get; set; }
        public string MortgageLoanValuationStatus { get; set; }
        public string MortgageLoanDirectDebitCheckStatus { get; set; }
        public bool IsSettlementAllowed { get; set; }
        public string MortgageLoanAmortizationStatus { get; set; }
        public string CompanyLoanAdditionalQuestionsStatus { get; set; }
        public IEnumerable<string> ListNames { get; set; }
        public string WorkflowVersion { get; set; }
        public bool HasLockedAgreement { get; set; }
        public bool IsLead { get; set; }
        public string CreditReportProviderName { get; set; }
        public string[] ListCreditReportProviders { get; set; }
    }
}