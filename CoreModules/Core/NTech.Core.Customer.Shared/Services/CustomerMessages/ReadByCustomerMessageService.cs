using NTech.Core.Customer.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer.Code.Services
{
    /// <summary>
    /// The Handled-concepts that messages have dont map very well to the customer side of messages
    /// since that is very context dependent.
    /// My pages has a common view of a family of ChannelType:s while each application has it's own dedicated channel. 
    /// For this reason we use a separate solution for tracking which messages have been read where
    /// we keep track of the latest read id by context and use that for filtering where
    /// the context is either a specific application or my pages generally.
    /// </summary>
    public class ReadByCustomerMessageService
    {
        private readonly IKeyValueStoreService keyValueStoreService;
        private readonly CustomerContextFactory contextFactory;

        public ReadByCustomerMessageService(IKeyValueStoreService keyValueStoreService, CustomerContextFactory contextFactory)
        {
            this.keyValueStoreService = keyValueStoreService;
            this.contextFactory = contextFactory;
        }
        /*
         So for the my pages usecase this would be used something like:
          GetNrOfUnreadMessages(42, 'MyPages', ['Credit_UnsecuredLoan', 'General', 'SavingsAccount_StandardAccount'], null, null)
        And for the application usecase:
          GetNrOfUnreadMessages(42, 'Application_A423423', null, 'Application_UnsecuredLoan', 'A423423')
         */
        public int GetNrOfUnreadMessages(int customerId, string readContext, List<string> onlyTheseChannelTypes, string channelType, string channelId)
        {
            var key = $"{readContext}_{customerId}";
            var latestReadIdRaw = keyValueStoreService.GetValue(key, KeyValueStoreKeySpaceCode.SecureMessageReadByCustomerV1.ToString());
            var latestReadId = latestReadIdRaw == null ? new int?() : int.Parse(latestReadIdRaw);

            using (var context = contextFactory.CreateContext())
            {
                var q = context.CustomerMessagesQueryable.Where(x => x.CustomerId == customerId && !x.IsFromCustomer);
                if (onlyTheseChannelTypes != null && onlyTheseChannelTypes.Count > 0)
                {
                    q = q.Where(x => onlyTheseChannelTypes.Contains(x.ChannelType));
                }
                if (!string.IsNullOrWhiteSpace(channelType))
                {
                    q = q.Where(x => x.ChannelType == channelType);
                }
                if (!string.IsNullOrWhiteSpace(channelId))
                {
                    q = q.Where(x => x.ChannelId == channelId);
                }
                if (latestReadId.HasValue)
                {
                    q = q.Where(x => x.Id > latestReadId.Value);
                }
                return q.Count();
            }
        }

        public void MarkAsRead(int customerId, string readContext, int latestReadMessageId, INTechCurrentUserMetadata user)
        {
            keyValueStoreService.SetValue($"{readContext}_{customerId}",
                KeyValueStoreKeySpaceCode.SecureMessageReadByCustomerV1.ToString(),
                latestReadMessageId.ToString(), user);
        }
    }
}