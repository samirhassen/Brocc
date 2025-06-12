using System.Linq;
using NTech.Core.Module.Shared.Database;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFixed;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;

namespace NTech.Core.Savings.Shared.Database
{
    public interface ISavingsContext : INTechDbContext, ISystemItemSavingsContext
    {
        IQueryable<SavingsAccountHeader> SavingsAccountHeadersQueryable { get; }
        IQueryable<LedgerAccountTransaction> LedgerAccountTransactionsQueryable { get; }
        IQueryable<IncomingPaymentHeader> IncomingPaymentHeadersQueryable { get; }
        IQueryable<KeyValueItem> KeyValueItemsQueryable { get; }
        IQueryable<OutgoingExportFileHeader> OutgoingExportFileHeadersQueryable { get; }
        IQueryable<FixedAccountProduct> FixedAccountProductQueryable { get; }

        void AddBusinessEvents(params BusinessEvent[] events);
        void AddSavingsAccountHeaders(params SavingsAccountHeader[] accounts);
        void AddSavingsAccountCreationRemarks(params SavingsAccountCreationRemark[] remarks);
        void AddSavingsAccountKycQuestions(params SavingsAccountKycQuestion[] questions);
        void AddDatedSavingsAccountStrings(params DatedSavingsAccountString[] strings);
        void AddSavingsAccountComments(params SavingsAccountComment[] comments);
        void AddSavingsAccountDocuments(params SavingsAccountDocument[] documents);
        void AddSavingsAccountKeySequences(params SavingsAccountKeySequence[] seqs);
        void AddOcrPaymentReferenceNrSequences(params OcrPaymentReferenceNrSequence[] seqs);
        void AddLedgerAccountTransactions(params LedgerAccountTransaction[] transactions);
        void AddIncomingPaymentHeaders(params IncomingPaymentHeader[] headers);
        void AddIncomingPaymentHeaderItems(params IncomingPaymentHeaderItem[] items);
        void AddIncomingPaymentFileHeaders(params IncomingPaymentFileHeader[] headers);
        void AddOutgoingPaymentHeaders(params OutgoingPaymentHeader[] headers);
        void AddOutgoingPaymentHeaderItems(params OutgoingPaymentHeaderItem[] items);
        void AddKeyValueItems(params KeyValueItem[] items);
        void RemoveKeyValueItems(params KeyValueItem[] items);
        void AddOutgoingExportFileHeaders(params OutgoingExportFileHeader[] items);

        int SaveChanges();
    }


    public interface ISystemItemSavingsContext
    {
        IQueryable<SystemItem> SystemItemsQueryable { get; }
        void AddSystemItems(params SystemItem[] items);
    }
}