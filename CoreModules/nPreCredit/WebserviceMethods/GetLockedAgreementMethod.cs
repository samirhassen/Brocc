using Newtonsoft.Json;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class GetLockedAgreementMethod : TypedWebserviceMethod<GetLockedAgreementMethod.Request, GetLockedAgreementMethod.Response>
    {
        public override string Path => "Agreement/Get-Locked";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var r = requestContext.Resolver();

            var lockedAgreement = r.Resolve<ILockedAgreementService>().GetLockedAgreement(request.ApplicationNr);

            return new Response
            {
                LockedAgreement = lockedAgreement
            };
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
        }

        public class Response
        {
            public LockedAgreementModel LockedAgreement { get; set; }
        }
    }
}