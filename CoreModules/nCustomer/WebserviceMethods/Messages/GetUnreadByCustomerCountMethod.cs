using nCustomer.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nCustomer.WebserviceMethods.Messages
{
    public class GetUnreadByCustomerCountMethod : TypedWebserviceMethod<GetUnreadByCustomerCountMethod.Request, GetUnreadByCustomerCountMethod.Response>
    {
        public override string Path => "CustomerMessage/GetUnreadByCustomerCount";

        public override bool IsEnabled => NEnv.IsSecureMessagesEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var service = requestContext.Service();
            var s = new ReadByCustomerMessageService(service.KeyValueStore, service.CustomerContextFactory);

            var unreadCount = s.GetNrOfUnreadMessages(
                request.CustomerId.Value,
                request.ReadContext,
                request.OnlyTheseChannelTypes,
                request.ChannelType,
                request.ChannelId);

            return new Response
            {
                UnreadCount = unreadCount
            };
        }

        public class Request
        {
            [Required]
            public int? CustomerId { get; set; }
            [Required]
            public string ReadContext { get; set; }
            public List<string> OnlyTheseChannelTypes { get; set; }
            public string ChannelType { get; set; }
            public string ChannelId { get; set; }
        }

        public class Response
        {
            public int UnreadCount { get; set; }
        }
    }
}