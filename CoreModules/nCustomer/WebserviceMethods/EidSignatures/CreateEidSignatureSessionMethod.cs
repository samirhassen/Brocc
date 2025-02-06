
using nCustomer.Services.EidSignatures;
using NTech.ElectronicSignatures;
using NTech.Services.Infrastructure.NTechWs;


namespace nCustomer.WebserviceMethods.EidSignatures
{
    public class CreateEidSignatureSessionMethod : TypedWebserviceMethod<SingleDocumentSignatureRequest, CreateEidSignatureSessionMethod.Response>
    {
        public override string Path => "ElectronicSignatures/Create-Session";

        public override bool IsEnabled => !string.IsNullOrWhiteSpace(NEnv.SignatureProvider);

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, SingleDocumentSignatureRequest request)
        {
            ValidateUsingAnnotations(request);

            var service = new EidSignatureService();

            var session = service.CreateSingleDocumentSignatureSession(request);

            return new Response
            {
                Session = session
            };
        }

        public class Response
        {
            public CommonElectronicIdSignatureSession Session { get; set; }
        }
    }
}