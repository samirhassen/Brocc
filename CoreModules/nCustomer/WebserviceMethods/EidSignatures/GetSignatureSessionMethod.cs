
using nCustomer.Services.EidSignatures;
using NTech.ElectronicSignatures;
using NTech.Services.Infrastructure.NTechWs;

namespace nCustomer.WebserviceMethods.EidSignatures
{
    public class GetSignatureSessionMethod : TypedWebserviceMethod<GetSignatureSessionMethod.Request, GetSignatureSessionMethod.Response>
    {
        public override string Path => "ElectronicSignatures/Get-Session";

        public override bool IsEnabled => !string.IsNullOrWhiteSpace(NEnv.SignatureProvider);

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            if (string.IsNullOrWhiteSpace(request.SessionId) == string.IsNullOrWhiteSpace(request.CustomSearchTermName))
            {
                return Error("Exactly one of SessionId and CustomSearchTermName+CustomSearchTermValue must be used");
            }

            var service = new EidSignatureService();
            var wasClosed = false;
            var session = service.SynchronizeSessionWithProvider(
                string.IsNullOrWhiteSpace(request.CustomSearchTermName) ? request.SessionId : request.CustomSearchTermValue,
                string.IsNullOrWhiteSpace(request.CustomSearchTermName) ? null : request.CustomSearchTermName,
                request.FirstCloseItIfOpen ?? false,
                observeWasClosed: x => wasClosed = x);

            if (session == null)
                return Error("No such session exists", httpStatusCode: 400, errorCode: "noSuchSessionExists");

            return new Response
            {
                Session = session,
                WasClosed = wasClosed
            };
        }

        public class Request
        {
            public string SessionId { get; set; }
            public string CustomSearchTermName { get; set; }
            public string CustomSearchTermValue { get; set; }
            public bool? FirstCloseItIfOpen { get; set; }
        }

        public class Response
        {
            public CommonElectronicIdSignatureSession Session { get; set; }
            public bool WasClosed { get; set; }
        }
    }
}