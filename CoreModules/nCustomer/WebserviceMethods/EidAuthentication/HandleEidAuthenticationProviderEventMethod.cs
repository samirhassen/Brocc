
using nCustomer.Code.Services.EidAuthentication;
using NTech.Services.Infrastructure.ElectronicAuthentication;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nCustomer.WebserviceMethods.EidSignatures
{
    public class HandleEidAuthenticationProviderEventMethod : TypedWebserviceMethod<HandleEidAuthenticationProviderEventMethod.Request, HandleEidAuthenticationProviderEventMethod.Response>
    {
        public override string Path => "ElectronicIdAuthentication/Handler-Provider-Event";

        public override bool IsEnabled => !string.IsNullOrWhiteSpace(NEnv.EidLoginProvider);

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var service = EidAuthenticationServiceFactory.CreateEidAuthenticationService();

            var result = service.HandleProviderLoginEvent(request.LocalSessionId, requestContext.CurrentUserMetadata(), request.ProviderEventData);

            if (result.Session == null)
                return Error("No such session exists", errorCode: "noSuchSessionExists", httpStatusCode: 400);

            return new Response
            {
                Session = result.Session,
                WasAuthenticated = result.WasAuthenticated
            };
        }

        public class Request
        {
            [Required]
            public string LocalSessionId { get; set; }

            public Dictionary<string, string> ProviderEventData { get; set; }
        }

        public class Response
        {
            public CommonElectronicAuthenticationSession Session { get; set; }
            public bool WasAuthenticated { get; set; } //This is just to prevent replay attacks of old sessions. This will be true at most once for each session.
        }
    }
}