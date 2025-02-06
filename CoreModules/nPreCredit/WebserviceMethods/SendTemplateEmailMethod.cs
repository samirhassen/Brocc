using NTech.Services.Infrastructure.Email;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods
{
    public class SendTemplateEmailMethod : TypedWebserviceMethod<SendTemplateEmailMethod.Request, SendTemplateEmailMethod.Response>
    {
        public override string Path => "Email/Send-Templated";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var s = requestContext.Resolver().Resolve<INTechEmailService>();
            s.SendTemplateEmail(request.ToEmails, request.TemplateName, request.Mines ?? new Dictionary<string, string>(), request.SendingContext ?? "SendTemplateEmail_" + DateTime.Now.ToString());

            return new Response
            {

            };
        }

        public class Request
        {
            [Required]
            public string TemplateName { get; set; }

            [Required]
            public List<string> ToEmails { get; set; }

            public Dictionary<string, string> Mines { get; set; }

            public string SendingContext { get; set; }
        }

        public class Response
        {

        }
    }
}