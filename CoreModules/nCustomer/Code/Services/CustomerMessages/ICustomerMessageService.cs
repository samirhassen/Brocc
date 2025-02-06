using System.Collections.Generic;

namespace nCustomer.Code.Services
{
    public interface ICustomerMessageService : INonSearchCustomerMessageService
    {
        List<MessageChannelModel> FindChannels(CustomerMessageChannelSearchTypeCode searchType, string searchText, NtechCurrentUserMetadata currentUser, bool includeGeneralChannels);
    }
}