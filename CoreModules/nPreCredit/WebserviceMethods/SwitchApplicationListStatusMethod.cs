using nPreCredit.Code.Services;
using NTech.Banking.Conversion;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods
{
    public class SwitchApplicationListStatusMethod : TypedWebserviceMethod<SwitchApplicationListStatusMethod.Request, SwitchApplicationListStatusMethod.Response>
    {
        public override string Path => "Application/Switch-ListStatus";

        public override bool IsEnabled => true;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            if (request.ListPrefixName.EndsWith("_"))
                return Error("ListPrefixName must not end with _"); //This is just go guard against the mistake of using CreditCheck_ instead of CreditCheck as the prefix since the list service adds a _ ... dont want two.


            var c = requestContext.Resolver().Resolve<CreditApplicationListService>();
            int? eventId = null;
            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                CreditApplicationEvent evt = null;
                if (!string.IsNullOrWhiteSpace(request.EventCode))
                {
                    var eventCode = Enums.Parse<CreditApplicationEventCode>(request.EventCode, ignoreCase: true);
                    if (!eventCode.HasValue)
                        return Error("Invalid EventCode", errorCode: "invalidEventCode");
                    evt = context.CreateAndAddEvent(eventCode.Value, applicationNr: request.ApplicationNr);
                }
                c.SwitchListStatusComposable(context, request.ListPrefixName, request.StatusName, applicationNr: request.ApplicationNr, evt: evt);
                if (!string.IsNullOrWhiteSpace(request.CommentText))
                {
                    context.CreateAndAddComment(request.CommentText?.Trim(), evt?.EventType ?? "SwitchApplicationListStatus", applicationNr: request.ApplicationNr);
                }

                context.SaveChanges();

                eventId = evt?.Id;
            }

            return new Response
            {
                EventId = eventId
            };
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
            [Required]
            public string ListPrefixName { get; set; }
            [Required]
            public string StatusName { get; set; }

            public string CommentText { get; set; }

            public string EventCode { get; set; }
        }

        public class Response
        {
            public int? EventId { get; set; }
        }
    }
}