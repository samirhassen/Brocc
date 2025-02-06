using nCustomer.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nCustomer.WebserviceMethods.Messages
{
    public class MarkMessagesReadByCustomerMethod : TypedWebserviceMethod<MarkMessagesReadByCustomerMethod.Request, MarkMessagesReadByCustomerMethod.Response>
    {
        public override string Path => "CustomerMessage/MarkAsReadByCustomer";

        public override bool IsEnabled => NEnv.IsSecureMessagesEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var service = requestContext.Service();
            var s = new ReadByCustomerMessageService(service.KeyValueStore, service.CustomerContextFactory);
            var user = requestContext.CurrentUserMetadata();

            s.MarkAsRead(request.CustomerId.Value, request.ReadContext, request.LatestReadMessageId.Value, user.CoreUser);

            return new Response
            {
            };
        }

        public class Request
        {
            [Required]
            public int? CustomerId { get; set; }
            [Required]
            public string ReadContext { get; set; }
            [Required]
            public int? LatestReadMessageId { get; set; }
        }

        public class Response
        {
        }
    }
}