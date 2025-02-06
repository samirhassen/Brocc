using Newtonsoft.Json;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.CompanyLoans;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class RemoveSignedCompanyLoanAgreementMethod : TypedWebserviceMethod<RemoveSignedCompanyLoanAgreementMethod.Request, RemoveSignedCompanyLoanAgreementMethod.Response>
    {
        public override string Path => "CompanyLoan/Remove-Signed-Agreement";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var ai = requestContext.Resolver().Resolve<ApplicationInfoService>().GetApplicationInfo(request.ApplicationNr);
            if (!ai.IsActive)
                return Error("Application is not active", errorCode: "applicationNotActive");

            var agreementService = requestContext.Resolver().Resolve<ICompanyLoanAgreementSignatureService>();
            agreementService.CancelSignedAgreementStep(ai);

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