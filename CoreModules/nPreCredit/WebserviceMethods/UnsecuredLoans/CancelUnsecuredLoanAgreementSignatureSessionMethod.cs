using nPreCredit.Code;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods
{
    public class CancelUnsecuredLoanAgreementSignatureSessionMethod : TypedWebserviceMethod<CancelUnsecuredLoanAgreementSignatureSessionMethod.Request, CancelUnsecuredLoanAgreementSignatureSessionMethod.Response>
    {
        public override string Path => "UnsecuredLoanApplication/Cancel-Signature-Session";

        public override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                context.CreateAndAddComment("Signatures reset", "UnsecuredLoans_SignaturesReset", applicationNr: request.ApplicationNr);

                KeyValueStoreService.RemoveValueComposable(context, request.ApplicationNr, "ActiveSignatureSession");

                AgreementSigningProvider.CancelSignatureSessionIfAny(request.ApplicationNr, NEnv.SignatureProvider == SignatureProviderCode.signicat, context);

                context.SaveChanges();
            }

            return new Response
            {

            };
        }

        public class Response
        {

        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
        }
    }
}