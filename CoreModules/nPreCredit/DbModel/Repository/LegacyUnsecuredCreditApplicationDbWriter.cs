using nPreCredit.Code.Services;
using nPreCredit.Code.Services.Comments;
using NTech;
using NTech.Core;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Core.PreCredit.Shared.Services;
using System;
using System.Collections.Generic;

namespace nPreCredit
{
    public class LegacyUnsecuredCreditApplicationDbWriter : ILegacyUnsecuredCreditApplicationDbWriter
    {
        private PreCreditContextExtended context;
        private readonly ICoreClock clock;
        private readonly INTechCurrentUserMetadata currentUser;
        private readonly IApplicationCommentServiceComposable commentService;

        public LegacyUnsecuredCreditApplicationDbWriter(ICoreClock clock, IClock legacyClock, INTechCurrentUserMetadata currentUser, IApplicationCommentServiceComposable commentService)
        {
            context = new PreCreditContextExtended(currentUser, legacyClock);
            context.BeginTransaction();
            this.clock = clock;
            this.currentUser = currentUser;
            this.commentService = commentService;
        }

        public void AddCreditApplicationHeader(CreditApplicationHeader header) =>
            context.CreditApplicationHeaders.Add(header);

        public void AddCreditApplicationItems(List<CreditApplicationItem> items) =>
            context.CreditApplicationItems.AddRange(items);

        public void AddCreditApplicationSearchTerms(List<CreditApplicationSearchTerm> items) =>
            context.CreditApplicationSearchTerms.AddRange(items);

        public CreditApplicationEvent CreateAndAddApplicationEvent(CreditApplicationEventCode eventCode) =>
            context.CreateEvent(eventCode);

        public void Dispose()
        {
            context.Dispose();
        }

        public void SaveChanges() => context.SaveChanges();

        public void Commit()
        {
            context.CommitTransaction();
        }

        public void SaveEncryptItems<T>(T[] items, Func<T, string> getClearTextValue, Action<T, long> setEncryptedValueId)
        {
            EncryptionService.SaveEncryptItemsShared(items, getClearTextValue, setEncryptedValueId,
                currentUser.UserId, NEnv.EncryptionKeys.CurrentKeyName, NEnv.EncryptionKeys.AsDictionary(), clock, context);
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
            if (!commentService.TryAddCommentComposable(applicationNr, commentText, eventType, attachment, out var failedMessage, context))
                throw new Exception(failedMessage);
        }
    }
}
