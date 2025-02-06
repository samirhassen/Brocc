using NTech.Core.Module.Shared.Infrastructure;
using System.Collections.Generic;

namespace nCustomer.Code.Services
{
    public interface INonSearchCustomerMessageService
    {
        CustomerMessageModel SaveCustomerMessage(int customerId, string channelType, string channelId, string text, string textFormat, bool isFromCustomer, int userid);

        void SendNewMessageNotification(CustomerMessageModel message, INTechCurrentUserMetadata currentUser);

        void FlagMessagesBeforeInChannelAsHandled(int messageId, int customerId, string channelType, string channelId, bool? isFromCustomer, int userid);

        FetchCustomerMessageModels GetCustomerMessages(int? customerId, string channelType, string channelId, bool includeMessageTexts, int? skipCount, int? takeCount, bool? isHandled, bool? isFromCustomer, List<string> onlyTheseChannelTypes);

        Dictionary<int, string> GetCustomerMessageTexts(List<int> messageIds, out Dictionary<int, string> messageTextFormat, out Dictionary<int, bool> isFromCustomerByMessageId, out Dictionary<int, string> attachedDocumentsByMessageId);

        List<MessageChannelModel> GetCustomerChannels(int customerId, bool includeGeneralChannel, List<string> onlyTheseChannelTypes);

        List<MessageChannelModel> SortChannels(List<MessageChannelModel> channels);

        CustomerMessageAttachedDocumentModel SaveCustomerMessageAttachedDocument(int messageId, string fileName, string contentTypeMimetype, string archiveKey);

        string HandleMessages(List<int> messageIds, int userid);
    }
}