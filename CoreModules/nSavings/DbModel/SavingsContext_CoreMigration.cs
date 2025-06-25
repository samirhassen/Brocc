using System.Linq;
using NTech.Core.Savings.Shared.Database;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFixed;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;

namespace nSavings.DbModel;

public partial class SavingsContext : ISavingsContext
{
    public IQueryable<SavingsAccountHeader> SavingsAccountHeadersQueryable => SavingsAccountHeaders;
    public IQueryable<SystemItem> SystemItemsQueryable => SystemItems;
    public IQueryable<LedgerAccountTransaction> LedgerAccountTransactionsQueryable => LedgerAccountTransactions;
    public IQueryable<IncomingPaymentHeader> IncomingPaymentHeadersQueryable => IncomingPaymentHeaders;
    public IQueryable<KeyValueItem> KeyValueItemsQueryable => KeyValueItems;
    public IQueryable<OutgoingExportFileHeader> OutgoingExportFileHeadersQueryable => OutgoingExportFileHeaders;
    public IQueryable<FixedAccountProduct> FixedAccountProductQueryable => FixedAccountProducts;

    public IQueryable<SavingsAccountInterestCapitalization> SavingsAccountInterestCapitalizationsQueryable =>
        SavingsAccountInterestCapitalizations;

    public IQueryable<SharedSavingsInterestRate> SharedSavingsInterestRatesQueryable => SharedSavingsInterestRates;

    public void AddBusinessEvents(params BusinessEvent[] events) => BusinessEvents.AddRange(events);

    public void AddDatedSavingsAccountStrings(params DatedSavingsAccountString[] strings) =>
        DatedSavingsAccountStrings.AddRange(strings);

    public void AddSavingsAccountComments(params SavingsAccountComment[] comments) =>
        SavingsAccountComments.AddRange(comments);

    public void AddSavingsAccountCreationRemarks(params SavingsAccountCreationRemark[] remarks) =>
        SavingsAccountCreationRemarks.AddRange(remarks);

    public void AddSavingsAccountDocuments(params SavingsAccountDocument[] documents) =>
        SavingsAccountDocuments.AddRange(documents);

    public void AddSavingsAccountHeaders(params SavingsAccountHeader[] accounts) =>
        SavingsAccountHeaders.AddRange(accounts);

    public void AddSavingsAccountKycQuestions(params SavingsAccountKycQuestion[] questions) =>
        SavingsAccountKycQuestions.AddRange(questions);

    public void AddSavingsAccountKeySequences(params SavingsAccountKeySequence[] seqs) =>
        SavingsAccountKeySequences.AddRange(seqs);

    public void AddOcrPaymentReferenceNrSequences(params OcrPaymentReferenceNrSequence[] seqs) =>
        OcrPaymentReferenceNrSequences.AddRange(seqs);

    public void AddSystemItems(params SystemItem[] items) => SystemItems.AddRange(items);

    public void AddLedgerAccountTransactions(params LedgerAccountTransaction[] transactions) =>
        LedgerAccountTransactions.AddRange(transactions);

    public void AddIncomingPaymentHeaders(params IncomingPaymentHeader[] headers) =>
        IncomingPaymentHeaders.AddRange(headers);

    public void AddIncomingPaymentHeaderItems(params IncomingPaymentHeaderItem[] items) =>
        IncomingPaymentHeaderItems.AddRange(items);

    public void AddIncomingPaymentFileHeaders(params IncomingPaymentFileHeader[] headers) =>
        IncomingPaymentFileHeaders.AddRange(headers);

    public void AddOutgoingPaymentHeaders(params OutgoingPaymentHeader[] headers) =>
        OutgoingPaymentHeaders.AddRange(headers);

    public void AddOutgoingPaymentHeaderItems(params OutgoingPaymentHeaderItem[] items) =>
        OutgoingPaymentHeaderItems.AddRange(items);

    public void AddKeyValueItems(params KeyValueItem[] items) => KeyValueItems.AddRange(items);
    public void RemoveKeyValueItems(params KeyValueItem[] items) => KeyValueItems.RemoveRange(items);

    public void AddOutgoingExportFileHeaders(params OutgoingExportFileHeader[] items) =>
        OutgoingExportFileHeaders.AddRange(items);

    public void AddSavingsAccountInterestCapitalizations(params SavingsAccountInterestCapitalization[] capitalization)
    {
        SavingsAccountInterestCapitalizations.AddRange(capitalization);
    }
}