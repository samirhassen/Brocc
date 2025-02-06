
using nCustomer.Code.Services.EidAuthentication;
using NTech.Services.Infrastructure.ElectronicAuthentication;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nCustomer.WebserviceMethods.EidSignatures
{
    public class CreateEidAuthenticationSessionMethod : TypedWebserviceMethod<CreateEidAuthenticationSessionMethod.Request, CreateEidAuthenticationSessionMethod.Response>
    {
        public override string Path => "ElectronicIdAuthentication/Create-Session";

        public override bool IsEnabled => !string.IsNullOrWhiteSpace(NEnv.EidLoginProvider);

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var service = EidAuthenticationServiceFactory.CreateEidAuthenticationService();

            if (!NEnv.BaseCivicRegNumberParser.TryParse(request.CivicRegNumber, out var civicRegNumber))
                return Error("Invalid CivicRegNumber", errorCode: "invalidCivicRegNumber");

            var session = service.CreateSession(civicRegNumber, new ReturnUrlModel(request.ReturnUrl), requestContext.CurrentUserMetadata(), request.CustomData);

            return new Response
            {
                Session = session
            };
        }

        public class Request
        {
            [Required]
            public string CivicRegNumber { get; set; }

            [Required]
            public string ReturnUrl { get; set; }

            public Dictionary<string, string> CustomData { get; set; }
        }

        public class Response
        {
            public CommonElectronicAuthenticationSession Session { get; set; }
        }
    }
}