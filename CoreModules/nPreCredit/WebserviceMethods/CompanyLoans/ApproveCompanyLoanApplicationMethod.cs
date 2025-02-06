using Newtonsoft.Json;
using nPreCredit.Code.Services.CompanyLoans;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class ApproveCompanyLoanApplicationMethod : TypedWebserviceMethod<ApproveCompanyLoanApplicationMethod.Request, ApproveCompanyLoanApplicationMethod.Response>
    {
        public override string Path => "CompanyLoan/Approve-Application";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var creditNr = requestContext.Resolver().Resolve<ICompanyLoanApplicationApprovalService>().ApproveApplicationAndCreateCredit(request.ApplicationNr);

            return new Response
            {
                CreditNr = creditNr
            };
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
        }

        public class Response
        {
            public string CreditNr { get; set; }
        }
    }
}