using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class CancelDirectDebitSignatureSessionMethod :
        TypedWebserviceMethod<CancelDirectDebitSignatureSessionMethod.Request, CancelDirectDebitSignatureSessionMethod.Response>
    {
        public override string Path => "UnsecuredLoanStandard/Cancel-DirectDebit-Signature-Session";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var directDebitService = requestContext.Resolver().Resolve<SwedishDirectDebitConsentDocumentService>();
            directDebitService.CancelDirectDebitSignatureSession(request.ApplicationNr);

            return new Response();
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
        }

        public class Response
        {

        }
    }
}