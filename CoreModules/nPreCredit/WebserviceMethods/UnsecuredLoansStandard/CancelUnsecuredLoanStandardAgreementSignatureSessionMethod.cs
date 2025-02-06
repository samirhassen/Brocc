using Newtonsoft.Json;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class CancelUnsecuredLoanStandardAgreementSignatureSessionMethod : TypedWebserviceMethod<CancelUnsecuredLoanStandardAgreementSignatureSessionMethod.Request, CancelUnsecuredLoanStandardAgreementSignatureSessionMethod.Response>
    {
        public override string Path => "UnsecuredLoanStandard/Cancel-Agreement-Signature-Session";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var resolver = requestContext.Resolver();

            var agreementService = resolver.Resolve<UnsecuredLoanStandardAgreementService>();
            agreementService.CancelSignatureSession(request.ApplicationNr, true);

            return new Response
            {

            };
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