using Newtonsoft.Json;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class ApproveLockedAgreementMethod : TypedWebserviceMethod<ApproveLockedAgreementMethod.Request, ApproveLockedAgreementMethod.Response>
    {
        public override string Path => "Agreement/Approve-Locked";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var r = requestContext.Resolver();

            var requireDuality = NEnv.IsProduction || !request.RequestOverrideDuality;

            var wasApproved = r.Resolve<ILockedAgreementService>().TryApprovedLockedAgreement(request.ApplicationNr, requireDuality, out var lockedAgreement);

            return new Response
            {
                WasApproved = wasApproved,
                LockedAgreement = lockedAgreement
            };
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
            public bool RequestOverrideDuality { get; set; }
        }

        public class Response
        {
            public bool WasApproved { get; set; }
            public LockedAgreementModel LockedAgreement { get; set; }
        }
    }
}