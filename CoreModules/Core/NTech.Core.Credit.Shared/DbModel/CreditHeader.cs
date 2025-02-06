using NTech.Core.Credit.Shared.DbModel;
using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nCredit
{
    public class CreditHeader : InfrastructureBaseItem
    {
        public string CreditNr { get; set; }
        public string ProviderName { get; set; }
        public string CreditType { get; set; }
        public int NrOfApplicants { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public string Status { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
        public int CreatedByBusinessEventId { get; set; }
        public CollateralHeader Collateral { get; set; }
        public int? CollateralHeaderId { get; set; }
        public virtual List<AccountTransaction> Transactions { get; set; }
        public virtual List<CreditReminderHeader> Reminders { get; set; }
        public virtual List<CreditTerminationLetterHeader> TerminationLetters { get; set; }
        public virtual List<CreditNotificationHeader> Notifications { get; set; }
        public virtual List<DatedCreditValue> DatedCreditValues { get; set; }
        public virtual List<DatedCreditCustomerValue> DatedCreditCustomerValues { get; set; }
        public virtual List<DatedCreditString> DatedCreditStrings { get; set; }
        public virtual List<DatedCreditDate> DatedCreditDates { get; set; }
        public virtual List<CreditCustomer> CreditCustomers { get; set; }
        public virtual List<CreditComment> Comments { get; set; }
        public virtual List<CreditPaymentFreeMonth> CreditPaymentFreeMonths { get; set; }
        public virtual List<CreditFuturePaymentFreeMonth> CreditFuturePaymentFreeMonths { get; set; }
        public virtual List<CreditDocument> Documents { get; set; }
        public virtual List<CreditTermsChangeHeader> TermsChanges { get; set; }
        public virtual List<CreditSettlementOfferHeader> CreditSettlementOffers { get; set; }
        public virtual List<EInvoiceFiAction> EInvoiceFiActions { get; set; }
        public virtual List<CreditSecurityItem> SecurityItems { get; set; }
        public virtual List<CreditOutgoingDirectDebitItem> CreditOutgoingDirectDebitItems { get; set; }
        public virtual List<CreditCustomerListMember> CustomerListMembers { get; set; }
        public virtual List<CreditCustomerListOperation> CustomerListOperations { get; set; }
        public virtual List<CreditAnnualStatementHeader> AnnualStatements { get; set; }
        public virtual List<AlternatePaymentPlanHeader> AlternatePaymentPlans { get; set; }
    }
}