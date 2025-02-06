
using nCustomer.Code.Services.EidAuthentication;
using NTech.Services.Infrastructure.ElectronicAuthentication;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nCustomer.WebserviceMethods.EidSignatures
{
    public class GetEidAuthenticationSessionMethod : TypedWebserviceMethod<GetEidAuthenticationSessionMethod.Request, GetEidAuthenticationSessionMethod.Response>
    {
        public override string Path => "ElectronicIdAuthentication/Get-Session";

        public override bool IsEnabled => !string.IsNullOrWhiteSpace(NEnv.EidLoginProvider);

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var service = new AuthenticationSessionService(requestContext.Clock());

            var result = service.GetSession(request.LocalSessionId);

            if (result == null)
                return Error("No such session exists", errorCode: "noSuchSessionExists", httpStatusCode: 400);

            return new Response
            {
                Session = result
            };
        }

        public class Request
        {
            [Required]
            public string LocalSessionId { get; set; }
        }

        public class Response
        {
            public CommonElectronicAuthenticationSession Session { get; set; }
        }
    }
}