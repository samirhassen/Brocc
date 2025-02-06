using nPreCredit;
using nPreCredit.Code.Services.Comments;
using System;
using System.Collections.Generic;

namespace NTech.Core.PreCredit.Shared.Services
{
    public interface ILegacyUnsecuredCreditApplicationDbWriter : IDisposable
    {
        void SaveEncryptItems<T>(
            T[] items,
            Func<T, string> getClearTextValue,
            Action<T, long> setEncryptedValueId);
        void AddMetadataOnlyComment(string applicationNr, string commentText, string eventType, MetadataOnlyCommentAttachment attachment);
        void AddCreditApplicationHeader(CreditApplicationHeader header);
        void AddCreditApplicationItems(List<CreditApplicationItem> items);
        void AddCreditApplicationSearchTerms(List<CreditApplicationSearchTerm> items);
        CreditApplicationEvent CreateAndAddApplicationEvent(CreditApplicationEventCode eventCode);
        bool SetUniqueComplexApplicationListItems(string applicationNr, string listName, int nr, Dictionary<string, string> namesAndValues);
        void StoreApplicationRequestJson(string applicationNr, string jsonRequest);
        void SaveChanges();
        void Commit();
    }
}
