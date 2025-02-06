using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nCustomer.WebserviceMethods.Messages
{
    public class GetCustomerMessageTextsMethod : TypedWebserviceMethod<GetCustomerMessageTextsMethod.Request, GetCustomerMessageTextsMethod.Response>
    {
        public override string Path => "CustomerMessage/GetMessageTexts";

        public override bool IsEnabled => NEnv.IsSecureMessagesEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);
            var messageTextByMessageId = requestContext.Service().CustomerMessage.GetCustomerMessageTexts(request.MessageIds, out var messageTextFormat, out var isFromCustomerByMessageId, out var attachedDocumentsByMessageId);

            return new Response
            {
                MessageTextByMessageId = messageTextByMessageId,
                MessageTextFormat = messageTextFormat,
                IsFromCustomerByMessageId = isFromCustomerByMessageId,
                AttachedDocumentsByMessageId = attachedDocumentsByMessageId
            };
        }

        public class Request
        {
            [Required]
            public List<int> MessageIds { get; set; }
        }

        public class Response
        {
            public Dictionary<int, string> MessageTextByMessageId { get; set; }
            public Dictionary<int, string> MessageTextFormat { get; set; }
            public Dictionary<int, bool> IsFromCustomerByMessageId { get; set; }
            public Dictionary<int, string> AttachedDocumentsByMessageId { get; set; }
        }
    }
}