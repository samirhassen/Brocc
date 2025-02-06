using Newtonsoft.Json;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.CompanyLoans;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class CreateCompanyLoanAgreementPrintContextMethod : TypedWebserviceMethod<CreateCompanyLoanAgreementPrintContextMethod.Request, CreateCompanyLoanAgreementPrintContextMethod.Response>
    {
        public override string Path => "CompanyLoan/Create-Agreement-PrintContext";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);
            var r = requestContext.Resolver();

            var ai = r.Resolve<ApplicationInfoService>().GetApplicationInfo(request.ApplicationNr);
            return new Response
            {
                PrintContext = r.Resolve<ICompanyLoanAgreementService>().GetPrintContext(ai)
            };
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
        }

        public class Response
        {
            public CompanyLoanAgreementPrintContextModel PrintContext { get; set; }
        }
    }
}