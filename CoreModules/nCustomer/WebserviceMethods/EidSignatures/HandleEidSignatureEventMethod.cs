
using nCustomer.Services.EidSignatures;
using NTech.ElectronicSignatures;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;

namespace nCustomer.WebserviceMethods.EidSignatures
{
    public class HandleEidSignatureEventMethod : TypedWebserviceMethod<HandleEidSignatureEventMethod.Request, HandleEidSignatureEventMethod.Response>
    {
        public override string Path => "ElectronicSignatures/Handle-Event";

        public override bool IsEnabled => !string.IsNullOrWhiteSpace(NEnv.SignatureProvider);

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var service = ProviderSignatureServiceFactory.CreateService();

            var session = service.HandleSignatureEvent(request.ProviderEventData);

            if (session == null)
                return Error("No such session exists", httpStatusCode: 400, errorCode: "noSuchSessionExists");

            return new Response
            {
                Session = session,
                ActiveSignatureUrlBySignerNr = session.GetActiveSignatureUrlBySignerNr()
            };
        }

        public class Request
        {
            public Dictionary<string, string> ProviderEventData { get; set; }
        }

        public class Response
        {
            public CommonElectronicIdSignatureSession Session { get; set; }
            public Dictionary<int, string> ActiveSignatureUrlBySignerNr { get; set; }
        }
    }
}