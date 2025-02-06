using Newtonsoft.Json;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class RemoveLockedAgreementMethod : TypedWebserviceMethod<RemoveLockedAgreementMethod.Request, RemoveLockedAgreementMethod.Response>
    {
        public override string Path => "Agreement/Remove-Locked";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var r = requestContext.Resolver();

            var wasUnlocked = r.Resolve<ILockedAgreementService>().UnlockAgreement(request.ApplicationNr);

            return new Response
            {
                WasRemoved = wasUnlocked
            };
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
        }

        public class Response
        {
            public bool WasRemoved { get; set; }
        }
    }
}