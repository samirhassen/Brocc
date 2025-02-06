using Microsoft.EntityFrameworkCore;
using nCredit;
using nCredit.DbModel.Model;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.DbModel;
using NTech.Core.Module.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;

namespace NTech.Core.Credit.Database
{
    public class CreditContextExtended : CreditContext, ICreditContextExtended
    {
        public CreditContextExtended(INTechCurrentUserMetadata currentUser, ICoreClock clock)
        {
            CurrentUser = currentUser;
            CoreClock = clock;
        }

        public INTechCurrentUserMetadata CurrentUser { get; }
        public ICoreClock CoreClock { get; }

        public T FillInfrastructureFields<T>(T b) where T : InfrastructureBaseItem
        {
            b.ChangedById = CurrentUser.UserId;
            b.ChangedDate = CoreClock.Now;
            b.InformationMetaData = CurrentUser.InformationMetadata;
            return b;
        }

        public IQueryable<CreditHeader> CreditHeadersQueryable => CreditHeaders;
        public IQueryable<CreditHeader> CreditHeadersQueryableNoTracking => CreditHeaders.AsNoTracking();
        public IQueryable<CreditCustomerListMember> CreditCustomerListMembersQueryable => CreditCustomerListMembers;
        public IQueryable<CreditOutgoingDirectDebitItem> CreditOutgoingDirectDebitItemsQueryable => CreditOutgoingDirectDebitItems;
        public IQueryable<CreditNotificationHeader> CreditNotificationHeadersQueryable => CreditNotificationHeaders;
        public IQueryable<IncomingPaymentHeader> IncomingPaymentHeadersQueryable => IncomingPaymentHeaders;
        public IQueryable<AccountTransaction> TransactionsQueryable => Transactions;
        public IQueryable<AccountTransaction> TransactionsIncludingBusinessEventQueryable => Transactions.Include(x => x.BusinessEvent);
        public IQueryable<CreditSettlementOfferHeader> CreditSettlementOfferHeadersQueryable => CreditSettlementOfferHeaders;
        public IQueryable<DatedCreditDate> DatedCreditDatesQueryable => DatedCreditDates;
        public IQueryable<DatedCreditString> DatedCreditStringsQueryable => DatedCreditStrings;
        public IQueryable<DatedCreditValue> DatedCreditValuesQueryable => DatedCreditValues;
        public IQueryable<IncomingPaymentHeaderItem> IncomingPaymentHeaderItemsQueryable => IncomingPaymentHeaderItems;
        public IQueryable<SharedDatedValue> SharedDatedValuesQueryable => SharedDatedValues;
        public IQueryable<KeyValueItem> KeyValueItemsQueryable => KeyValueItems;
        public IQueryable<CreditCustomer> CreditCustomersQueryable => CreditCustomers;
        public IQueryable<CreditPaymentFreeMonth> CreditPaymentFreeMonthsQueryable => CreditPaymentFreeMonths;
        public IQueryable<CreditFuturePaymentFreeMonth> CreditFuturePaymentFreeMonthsQueryable => CreditFuturePaymentFreeMonths;
        public IQueryable<IncomingPaymentFileHeader> IncomingPaymentFileHeadersQueryable => IncomingPaymentFileHeaders;
        public IQueryable<CreditTermsChangeHeader> CreditTermsChangeHeadersQueryable => CreditTermsChangeHeaders;
        public IQueryable<CreditComment> CreditCommentsQueryable => CreditComments;
        public IQueryable<CreditReminderHeader> CreditReminderHeadersQueryable => CreditReminderHeaders;
        public IQueryable<CreditTerminationLetterHeader> CreditTerminationLetterHeadersQueryable => CreditTerminationLetterHeaders;
        public IQueryable<OutgoingDebtCollectionFileHeader> OutgoingDebtCollectionFileHeadersQueryable => OutgoingDebtCollectionFileHeaders;
        public IQueryable<CollateralHeader> CollateralHeadersQueryable => CollateralHeaders;
        public IQueryable<FixedMortgageLoanInterestRate> FixedMortgageLoanInterestRatesQueryable => FixedMortgageLoanInterestRates;
        public IQueryable<BusinessEvent> BusinessEventsQueryable => BusinessEvents;
        public IQueryable<SystemItem> SystemItemsQueryable => SystemItems;
        public IQueryable<OutgoingExportFileHeader> OutgoingExportFileHeaderQueryable => OutgoingExportFileHeader;
        public IQueryable<CreditAnnualStatementHeader> CreditAnnualStatementHeadersQueryable => CreditAnnualStatementHeaders;
        public IQueryable<OutgoingAmlMonitoringExportFileHeader> OutgoingAmlMonitoringExportFileHeadersQueryable => OutgoingAmlMonitoringExportFileHeaders;
        public IQueryable<OutgoingBookkeepingFileHeader> OutgoingBookkeepingFileHeadersQueryable => OutgoingBookkeepingFileHeaders;
        public IQueryable<CreditDocument> CreditDocumentsQueryable => Documents;
        public IQueryable<AlternatePaymentPlanHeader> AlternatePaymentPlanHeadersQueryable => AlternatePaymentPlanHeaders;
        public IQueryable<IncomingDirectDebitStatusChangeFileHeader> IncomingDirectDebitStatusChangeFileHeadersQueryable => IncomingDirectDebitStatusChangeFileHeaders;
        public IQueryable<CreditSecurityItem> CreditSecurityItemsQueryable => CreditSecurityItems;
        public IQueryable<OutgoingPaymentHeader> OutgoingPaymentHeadersQueryable => OutgoingPaymentHeaders;
        public IQueryable<EInvoiceFiAction> EInvoiceFiActionsQueryable => EInvoiceFiActions;
        public IQueryable<EInvoiceFiMessageHeader> EInvoiceFiMessageHeadersQueryable => EInvoiceFiMessageHeaders;

        public void AddCreditSecurityItem(CreditSecurityItem item) => CreditSecurityItems.Add(item);
        public void AddCreditComment(CreditComment comment) => CreditComments.Add(comment);
        public void AddBusinessEvent(BusinessEvent evt) => BusinessEvents.Add(evt);
        public void AddDatedCreditValue(DatedCreditValue datedCreditValue) => DatedCreditValues.Add(datedCreditValue);
        public void AddDatedCreditCustomerValue(DatedCreditCustomerValue datedCreditCustomerValue) => DatedCreditCustomerValues.Add(datedCreditCustomerValue);
        public void AddDatedCreditString(DatedCreditString datedCreditString) => DatedCreditStrings.Add(datedCreditString);
        public void AddDatedCreditDate(DatedCreditDate datedCreditDate) => DatedCreditDates.Add(datedCreditDate);
        public void AddCreditDocument(CreditDocument creditDocument) => Documents.Add(creditDocument);
        public void AddCreditCustomerListMember(CreditCustomerListMember creditCustomerListMember) => CreditCustomerListMembers.Add(creditCustomerListMember);
        public void RemoveCreditCustomerListMember(CreditCustomerListMember creditCustomerListMember) => CreditCustomerListMembers.Remove(creditCustomerListMember);
        public void AddCreditCustomerListOperation(CreditCustomerListOperation creditCustomerListOperation) => CreditCustomerListOperations.Add(creditCustomerListOperation);
        public void AddCreditHeader(CreditHeader creditHeader) => CreditHeaders.Add(creditHeader);
        public void AddCreditCustomer(CreditCustomer creditCustomer) => CreditCustomers.Add(creditCustomer);
        public void AddAccountTransactions(params AccountTransaction[] transactions) => Transactions.AddRange(transactions);
        public void AddCreditOutgoingDirectDebitItem(CreditOutgoingDirectDebitItem outgoingDirectDebitItem) => CreditOutgoingDirectDebitItems.Add(outgoingDirectDebitItem);
        public void AddOutgoingPaymentHeader(OutgoingPaymentHeader outgoingPaymentHeader) => OutgoingPaymentHeaders.Add(outgoingPaymentHeader);
        public void AddOutgoingPaymentHeaderItem(OutgoingPaymentHeaderItem outgoingPaymentHeaderItem) => OutgoingPaymentHeaderItems.Add(outgoingPaymentHeaderItem);
        public void AddIncomingDirectDebitStatusChangeFileHeader(IncomingDirectDebitStatusChangeFileHeader incomingDirectDebitStatusChangeFileHeader) =>
            IncomingDirectDebitStatusChangeFileHeaders.Add(incomingDirectDebitStatusChangeFileHeader);
        public void AddOcrPaymentReferenceNrSequence(OcrPaymentReferenceNrSequence sequence) => OcrPaymentReferenceNrSequences.Add(sequence);
        public void AddIncomingPaymentHeader(IncomingPaymentHeader header) => IncomingPaymentHeaders.Add(header);
        public void AddIncomingPaymentHeaderItem(IncomingPaymentHeaderItem item) => IncomingPaymentHeaderItems.Add(item);
        public void AddIncomingPaymentFileHeader(IncomingPaymentFileHeader header) => IncomingPaymentFileHeaders.Add(header);
        public void AddWriteoffHeader(WriteoffHeader header) => WriteoffHeaders.Add(header);
        public void AddCollateralHeader(CollateralHeader header) => CollateralHeaders.Add(header);
        public void AddCollateralItems(params CollateralItem[] collateralItems) => CollateralItems.AddRange(collateralItems);
        public void AddKeyValueItems(params KeyValueItem[] keyValueItems) => KeyValueItems.AddRange(keyValueItems);
        public void RemoveKeyValueItem(KeyValueItem keyValueItem) => KeyValueItems.Remove(keyValueItem);
        public void AddCreditPaymentFreeMonths(params CreditPaymentFreeMonth[] creditPaymentFreeMonths) => CreditPaymentFreeMonths.AddRange(creditPaymentFreeMonths);
        public void AddCreditNotificationHeaders(params CreditNotificationHeader[] creditNotificationHeaders) => CreditNotificationHeaders.AddRange(creditNotificationHeaders);
        public void AddCreditSettlementOfferHeaders(params CreditSettlementOfferHeader[] creditSettlementOfferHeaders) => CreditSettlementOfferHeaders.AddRange(creditSettlementOfferHeaders);
        public void AddCreditSettlementOfferItems(params CreditSettlementOfferItem[] creditSettlementOfferItems) => CreditSettlementOfferItems.AddRange(creditSettlementOfferItems);
        public void AddCreditTermsChangeHeaders(params CreditTermsChangeHeader[] headers) => CreditTermsChangeHeaders.AddRange(headers);
        public void AddCreditTermsChangeItems(params CreditTermsChangeItem[] items) => CreditTermsChangeItems.AddRange(items);
        public void RemoveCreditTermsChangeItems(CreditTermsChangeItem item) => CreditTermsChangeItems.Remove(item);
        public void AddCreditReminderHeaders(params CreditReminderHeader[] headers) => CreditReminderHeaders.AddRange(headers);
        public void AddWriteoffHeaders(params WriteoffHeader[] headers) => WriteoffHeaders.AddRange(headers);
        public void AddOutgoingDebtCollectionFileHeaders(params OutgoingDebtCollectionFileHeader[] headers) => OutgoingDebtCollectionFileHeaders.AddRange(headers);
        public void AddCreditTerminationLetterHeaders(params CreditTerminationLetterHeader[] headers) => CreditTerminationLetterHeaders.AddRange(headers);
        public void AddReferenceInterestChangeHeaders(params ReferenceInterestChangeHeader[] headers) => ReferenceInterestChangeHeaders.AddRange(headers);
        public void AddSharedDatedValues(params SharedDatedValue[] sharedDatedValues) => SharedDatedValues.AddRange(sharedDatedValues);
        public void AddDatedCreditValues(params DatedCreditValue[] datedCreditValues) => DatedCreditValues.AddRange(datedCreditValues);
        public void AddDatedCreditDates(params DatedCreditDate[] datedCreditDates) => DatedCreditDates.AddRange(datedCreditDates);
        public void AddSystemItems(params SystemItem[] systemItems) => SystemItems.AddRange(systemItems);
        public void AddCreditAnnualStatementHeaders(params CreditAnnualStatementHeader[] creditAnnualStatements) => CreditAnnualStatementHeaders.AddRange(creditAnnualStatements);
        public void AddFixedMortgageLoanInterestRates(params FixedMortgageLoanInterestRate[] rates) => FixedMortgageLoanInterestRates.AddRange(rates);
        public void RemoveFixedMortgageLoanInterestRates(params FixedMortgageLoanInterestRate[] rates) => FixedMortgageLoanInterestRates.RemoveRange(rates);
        public void AddHFixedMortgageLoanInterestRates(params HFixedMortgageLoanInterestRate[] rates) => HFixedMortgageLoanInterestRates.AddRange(rates);
        public void AddOutgoingAmlMonitoringExportFileHeaders(params OutgoingAmlMonitoringExportFileHeader[] headers) => OutgoingAmlMonitoringExportFileHeaders.AddRange(headers);
        public void AddOutgoingBookkeepingFileHeaders(params OutgoingBookkeepingFileHeader[] headers) => OutgoingBookkeepingFileHeaders.AddRange(headers);
        public void RemoveCreditCustomers(params CreditCustomer[] customers) => CreditCustomers.RemoveRange(customers);
        public void AddAlternatePaymentPlanHeaders(params AlternatePaymentPlanHeader[] headers) => AlternatePaymentPlanHeaders.AddRange(headers);
        public void AddCreditKeySequences(params CreditKeySequence[] keySequences) => CreditKeySequences.AddRange(keySequences);
        public void AddIncomingDirectDebitStatusChangeFileHeaders(params IncomingDirectDebitStatusChangeFileHeader[] headers) => IncomingDirectDebitStatusChangeFileHeaders.AddRange(headers);
        public void AddOutgoingPaymentFileHeaders(params OutgoingPaymentFileHeader[] headers) => OutgoingPaymentFileHeaders.AddRange(headers);
        public void AddEInvoiceFiActions(params EInvoiceFiAction[] actions) => EInvoiceFiActions.AddRange(actions);
        public void AddEInvoiceFiMessageHeaders(params EInvoiceFiMessageHeader[] headers) => EInvoiceFiMessageHeaders.AddRange(headers);
        public void AddCreditFuturePaymentFreeMonths(params CreditFuturePaymentFreeMonth[] months) => CreditFuturePaymentFreeMonths.AddRange(months);
        public void AddSieFileVerifications(params SieFileVerification[] verifications) => SieFileVerifications.AddRange(verifications);
        public void AddSieFileTransactions(params SieFileTransaction[] transactions) => SieFileTransactions.AddRange(transactions);

        public bool HasNewCreditAddedToContext(string creditNr) =>
            ChangeTracker.Entries<CreditHeader>().Any(x => x.State == EntityState.Added && x.Entity.CreditNr == creditNr);
    }
}
