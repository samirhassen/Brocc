using nPreCredit;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.Comments;
using NTech.Core.Module.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Core.PreCredit.Database;
using NTech.Core.PreCredit.Shared.Services;

namespace NTech.Core.PreCredit.Services
{
    public class CoreLegacyUnsecuredCreditApplicationDbWriter : ILegacyUnsecuredCreditApplicationDbWriter
    {
        private readonly INTechCurrentUserMetadata currentUser;
        private readonly ICoreClock clock;
        private readonly EncryptionService encryptionService;
        private PreCreditContextExtended context;

        public CoreLegacyUnsecuredCreditApplicationDbWriter(INTechCurrentUserMetadata currentUser, ICoreClock clock, EncryptionService encryptionService)
        {
            context = new PreCreditContextExtended(currentUser, clock);
            context.BeginTransaction();
            this.currentUser = currentUser;
            this.clock = clock;
            this.encryptionService = encryptionService;
        }

        public void AddCreditApplicationHeader(CreditApplicationHeader header)
        {
            context.CreditApplicationHeaders.Add(header);
        }

        public void AddCreditApplicationItems(List<CreditApplicationItem> items)
        {
            context.CreditApplicationItems.AddRange(items);
        }

        public void AddCreditApplicationSearchTerms(List<CreditApplicationSearchTerm> items)
        {
            context.CreditApplicationSearchTerms.AddRange(items);
        }

        public void Commit()
        {
            context.CommitTransaction();
        }

        public CreditApplicationEvent CreateAndAddApplicationEvent(CreditApplicationEventCode eventCode)
        {
            var evt = new CreditApplicationEvent
            {
                EventType = eventCode.ToString(),
                EventDate = clock.Now,
                TransactionDate = clock.Today
            }.PopulateInfraFields(currentUser, clock);

            context.CreditApplicationEvents.Add(evt);

            return evt;
        }

        public void Dispose()
        {
            context.Dispose();
        }

        public void SaveChanges()
        {
            context.SaveChanges();
        }

        public void SaveEncryptItems<T>(T[] items, Func<T, string> getClearTextValue, Action<T, long> setEncryptedValueId)
        {
            encryptionService.SaveEncryptItems(items, getClearTextValue, setEncryptedValueId, context);
        }

        public bool SetUniqueComplexApplicationListItems(string applicationNr, string listName, int nr, Dictionary<string, string> namesAndValues) =>
            ComplexApplicationListService.SetUniqueItems(
                applicationNr, listName, nr,
                namesAndValues,
                context);

        public void StoreApplicationRequestJson(string applicationNr, string jsonRequest) =>
            context.CreateAndAddKeyValueItem(KeyValueStoreKeySpaceCode.ExternalApplicationRequestJson.ToString(), applicationNr, jsonRequest);

        public void AddMetadataOnlyComment(string applicationNr, string commentText, string eventType, MetadataOnlyCommentAttachment attachment)
        {
            var attachmentStorage = CreditApplicationCommentAttachmentStorageModel.CreateMetadataOnly(attachment);
            context.CreditApplicationComments.Add(new CreditApplicationComment
            {
                ApplicationNr = applicationNr,
                Attachment = attachmentStorage?.Serialize(),
                CommentById = currentUser.UserId,
                CommentDate = clock.Now,
                CommentText = commentText,
                EventType = eventType
            }.PopulateInfraFields(currentUser, clock));
        }
    }
}
