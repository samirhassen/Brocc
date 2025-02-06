using nCredit;
using nCredit.DbModel.Model;
using NTech.Core.Credit.Shared.DbModel;
using NTech.Core.Module.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System.Linq;


namespace NTech.Core.Credit.Shared.Database
{
    public interface ICreditContextExtended : INTechDbContext, ISystemItemCreditContext
    {
        IQueryable<CreditHeader> CreditHeadersQueryable { get; }
        IQueryable<CreditHeader> CreditHeadersQueryableNoTracking { get; }
        IQueryable<CreditCustomerListMember> CreditCustomerListMembersQueryable { get; }
        IQueryable<CreditOutgoingDirectDebitItem> CreditOutgoingDirectDebitItemsQueryable { get; }
        IQueryable<CreditNotificationHeader> CreditNotificationHeadersQueryable { get; }
        IQueryable<IncomingPaymentHeader> IncomingPaymentHeadersQueryable { get; }
        IQueryable<AccountTransaction> TransactionsQueryable { get; }
        IQueryable<AccountTransaction> TransactionsIncludingBusinessEventQueryable { get; }
        IQueryable<CreditSettlementOfferHeader> CreditSettlementOfferHeadersQueryable { get; }
        IQueryable<DatedCreditDate> DatedCreditDatesQueryable { get; }
        IQueryable<DatedCreditString> DatedCreditStringsQueryable { get; }
        IQueryable<DatedCreditValue> DatedCreditValuesQueryable { get; }
        IQueryable<IncomingPaymentHeaderItem> IncomingPaymentHeaderItemsQueryable { get; }
        IQueryable<SharedDatedValue> SharedDatedValuesQueryable { get; }
        IQueryable<KeyValueItem> KeyValueItemsQueryable { get; }
        IQueryable<CreditCustomer> CreditCustomersQueryable { get; }
        IQueryable<CreditPaymentFreeMonth> CreditPaymentFreeMonthsQueryable { get; }
        IQueryable<CreditFuturePaymentFreeMonth> CreditFuturePaymentFreeMonthsQueryable { get; }
        IQueryable<IncomingPaymentFileHeader> IncomingPaymentFileHeadersQueryable { get; }
        IQueryable<CreditTermsChangeHeader> CreditTermsChangeHeadersQueryable { get; }
        IQueryable<CreditComment> CreditCommentsQueryable { get; }
        IQueryable<CreditReminderHeader> CreditReminderHeadersQueryable { get; }
        IQueryable<CreditTerminationLetterHeader> CreditTerminationLetterHeadersQueryable { get; }
        IQueryable<OutgoingDebtCollectionFileHeader> OutgoingDebtCollectionFileHeadersQueryable { get; }
        IQueryable<CollateralHeader> CollateralHeadersQueryable { get; }
        IQueryable<FixedMortgageLoanInterestRate> FixedMortgageLoanInterestRatesQueryable { get; }
        IQueryable<BusinessEvent> BusinessEventsQueryable { get; }
        IQueryable<OutgoingExportFileHeader> OutgoingExportFileHeaderQueryable { get; }
        IQueryable<CreditAnnualStatementHeader> CreditAnnualStatementHeadersQueryable { get; }
        IQueryable<OutgoingAmlMonitoringExportFileHeader> OutgoingAmlMonitoringExportFileHeadersQueryable { get; }
        IQueryable<OutgoingBookkeepingFileHeader> OutgoingBookkeepingFileHeadersQueryable { get; }
        IQueryable<CreditDocument> CreditDocumentsQueryable { get; }
        IQueryable<AlternatePaymentPlanHeader> AlternatePaymentPlanHeadersQueryable { get; }
        IQueryable<IncomingDirectDebitStatusChangeFileHeader> IncomingDirectDebitStatusChangeFileHeadersQueryable { get; }
        IQueryable<CreditSecurityItem> CreditSecurityItemsQueryable { get; }
        IQueryable<OutgoingPaymentHeader> OutgoingPaymentHeadersQueryable { get; }
        IQueryable<EInvoiceFiAction> EInvoiceFiActionsQueryable { get; }
        IQueryable<EInvoiceFiMessageHeader> EInvoiceFiMessageHeadersQueryable { get; }

        void AddCreditSecurityItem(CreditSecurityItem item);
        void AddCreditComment(CreditComment comment);
        void AddBusinessEvent(BusinessEvent evt);
        void AddDatedCreditValue(DatedCreditValue datedCreditValue);
        void AddDatedCreditCustomerValue(DatedCreditCustomerValue datedCreditCustomerValue);
        void AddDatedCreditString(DatedCreditString datedCreditString);
        void AddDatedCreditDate(DatedCreditDate datedCreditDate);
        void AddCreditDocument(CreditDocument creditDocument);
        void AddCreditCustomerListMember(CreditCustomerListMember creditCustomerListMember);
        void RemoveCreditCustomerListMember(CreditCustomerListMember creditCustomerListMember);
        void AddCreditCustomerListOperation(CreditCustomerListOperation creditCustomerListOperation);
        void AddCreditHeader(CreditHeader creditHeader);
        void AddCreditCustomer(CreditCustomer creditCustomer);
        void AddAccountTransactions(params AccountTransaction[] transactions);
        void AddCreditOutgoingDirectDebitItem(CreditOutgoingDirectDebitItem outgoingDirectDebitItem);
        void AddOutgoingPaymentHeader(OutgoingPaymentHeader outgoingPaymentHeader);
        void AddOutgoingPaymentHeaderItem(OutgoingPaymentHeaderItem outgoingPaymentHeaderItem);
        void AddIncomingDirectDebitStatusChangeFileHeader(IncomingDirectDebitStatusChangeFileHeader incomingDirectDebitStatusChangeFileHeader);
        void AddOcrPaymentReferenceNrSequence(OcrPaymentReferenceNrSequence sequence);
        void AddIncomingPaymentFileHeader(IncomingPaymentFileHeader incomingPaymentFileHeader);
        void AddIncomingPaymentHeaderItem(IncomingPaymentHeaderItem item);
        void AddIncomingPaymentHeader(IncomingPaymentHeader header);
        void AddWriteoffHeader(WriteoffHeader header);
        void AddCollateralHeader(CollateralHeader header);
        void AddCollateralItems(params CollateralItem[] collateralItems);
        void AddKeyValueItems(params KeyValueItem[] keyValueItems);
        void RemoveKeyValueItem(KeyValueItem keyValueItem);
        void AddCreditPaymentFreeMonths(params CreditPaymentFreeMonth[] creditPaymentFreeMonths);
        void AddCreditNotificationHeaders(params CreditNotificationHeader[] creditNotificationHeaders);
        void AddCreditSettlementOfferHeaders(params CreditSettlementOfferHeader[] creditSettlementOfferHeaders);
        void AddCreditSettlementOfferItems(params CreditSettlementOfferItem[] creditSettlementOfferItems);
        void AddCreditTermsChangeHeaders(params CreditTermsChangeHeader[] headers);
        void AddCreditTermsChangeItems(params CreditTermsChangeItem[] items);
        void RemoveCreditTermsChangeItems(CreditTermsChangeItem item);
        void AddCreditReminderHeaders(params CreditReminderHeader[] headers);
        void AddWriteoffHeaders(params WriteoffHeader[] headers);
        void AddOutgoingDebtCollectionFileHeaders(params OutgoingDebtCollectionFileHeader[] headers);
        void AddCreditTerminationLetterHeaders(params CreditTerminationLetterHeader[] headers);
        void AddReferenceInterestChangeHeaders(params ReferenceInterestChangeHeader[] headers);
        void AddSharedDatedValues(params SharedDatedValue[] sharedDatedValues);
        void AddDatedCreditValues(params DatedCreditValue[] datedCreditValues);
        void AddDatedCreditDates(params DatedCreditDate[] datedCreditDates);
        void AddCreditAnnualStatementHeaders(params CreditAnnualStatementHeader[] creditAnnualStatements);
        void AddFixedMortgageLoanInterestRates(params FixedMortgageLoanInterestRate[] rates);
        void RemoveFixedMortgageLoanInterestRates(params FixedMortgageLoanInterestRate[] rates);
        void AddHFixedMortgageLoanInterestRates(params HFixedMortgageLoanInterestRate[] rates);
        void AddOutgoingAmlMonitoringExportFileHeaders(params OutgoingAmlMonitoringExportFileHeader[] headers);
        void AddOutgoingBookkeepingFileHeaders(params OutgoingBookkeepingFileHeader[] headers);
        void AddAlternatePaymentPlanHeaders(params AlternatePaymentPlanHeader[] headers);
        void RemoveCreditCustomers(params CreditCustomer[] customers);
        void AddCreditKeySequences(params CreditKeySequence[] keySequences);
        void AddIncomingDirectDebitStatusChangeFileHeaders(params IncomingDirectDebitStatusChangeFileHeader[] headers);
        void AddOutgoingPaymentFileHeaders(params OutgoingPaymentFileHeader[] headers);
        void AddEInvoiceFiActions(params EInvoiceFiAction[] actions);
        void AddEInvoiceFiMessageHeaders(params EInvoiceFiMessageHeader[] headers);
        void AddCreditFuturePaymentFreeMonths(params CreditFuturePaymentFreeMonth[] months);
        void AddSieFileVerifications(params SieFileVerification[] verifications);
        void AddSieFileTransactions(params SieFileTransaction[] transactions);

        int SaveChanges();
        bool HasNewCreditAddedToContext(string creditNr);
        T FillInfrastructureFields<T>(T b) where T : InfrastructureBaseItem;
        ICoreClock CoreClock { get; }
        INTechCurrentUserMetadata CurrentUser { get; }
    }

    public interface ISystemItemCreditContext
    {
        IQueryable<SystemItem> SystemItemsQueryable { get; }
        void AddSystemItems(params SystemItem[] items);
    }
}
