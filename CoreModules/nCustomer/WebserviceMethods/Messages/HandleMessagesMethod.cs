using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nCustomer.WebserviceMethods.Messages
{
    public class HandleMessagesMethod : TypedWebserviceMethod<HandleMessagesMethod.Request, HandleMessagesMethod.Response>
    {
        public override string Path => "CustomerMessage/HandleMessages";

        public override bool IsEnabled => NEnv.IsSecureMessagesEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var s = requestContext.Service().CustomerMessage;
            var user = requestContext.CurrentUserMetadata();
            var userid = user.UserId;

            var status = s.HandleMessages(
              request.MessageIds, userid);

            var r = new Response
            {
                Status = status

            };

            return r;
        }

        public class Request
        {
            [Required]
            public List<int> MessageIds { get; set; }
        }

        public class Response
        {
            public string Status { get; set; }

        }
    }
}