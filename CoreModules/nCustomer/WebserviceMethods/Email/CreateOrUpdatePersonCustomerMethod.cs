using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCustomer.WebserviceMethods.Company
{
    public class SendTemplateEmailMethod : TypedWebserviceMethod<SendTemplateEmailMethod.Request, SendTemplateEmailMethod.Response>
    {
        public override string Path => "Email/Send-Templated";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var cc = request.ToCustomerIds != null ? request.ToCustomerIds.Count : 0;
            var ec = request.ToEmails != null ? request.ToEmails.Count : 0;

            if ((cc == 0 && ec == 0) || (cc > 0 && ec > 0))
                return Error("Exactly one of ToCustomerIds and ToEmails is required", errorCode: "missingRequiredField");

            List<string> emails;

            if (ec > 0)
            {
                emails = request.ToEmails;
            }
            else
            {
                emails = new List<string>();
                var r = requestContext.Service().Customer.BulkFetch(request.ToCustomerIds.ToHashSet(), new HashSet<string> { "email" }, requestContext.CurrentUserMetadata());
                foreach (var customerId in request.ToCustomerIds)
                {
                    if (r.ContainsKey(customerId) && r[customerId].Count == 1)
                        emails.Add(r[customerId].Single().Value);
                    else
                        return Error("At least one customer in ToCustomerIds has no email", errorCode: "missingCustomerEmail");
                }
            }

            var s = requestContext.Service().Email;
            s.SendTemplateEmail(emails, request.TemplateName, request.Mines ?? new Dictionary<string, string>(), request.SendingContext);

            return new Response
            {
            };
        }

        public class Request
        {
            //One of these two is required
            public List<int> ToCustomerIds { get; set; }

            public List<string> ToEmails { get; set; }

            [Required]
            public string TemplateName { get; set; }

            public Dictionary<string, string> Mines { get; set; }

            [Required]
            public string SendingContext { get; set; }
        }

        public class Response
        {
        }
    }
}