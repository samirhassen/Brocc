using Newtonsoft.Json;
using nPreCredit.Code.Services.CompanyLoans;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class CancelCompanyLoanAgreementSignatureSessionMethod : TypedWebserviceMethod<CancelCompanyLoanAgreementSignatureSessionMethod.Request, CancelCompanyLoanAgreementSignatureSessionMethod.Response>
    {
        public override string Path => "CompanyLoan/Cancel-Agreement-Signature-Session";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var s = requestContext.Resolver().Resolve<ICompanyLoanAgreementSignatureService>();

            var wasCancelled = s.CancelAnyActiveSignatureSessionByApplicationNr(request.ApplicationNr);

            return new Response
            {
                WasCancelled = wasCancelled
            };
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
        }

        public class Response
        {
            public bool WasCancelled { get; set; }
        }
    }
}