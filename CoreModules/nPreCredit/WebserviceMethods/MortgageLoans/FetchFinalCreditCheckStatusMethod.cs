using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;

namespace nPreCredit.WebserviceMethods.MortgageLoans
{

    public class FetchFinalCreditCheckStatusMethod : TypedWebserviceMethod<FetchFinalCreditCheckStatusMethod.Request, MortgageLoanApplicationFinalCreditCheckStatusModel>
    {
        public override string Path => "MortgageLoan/CreditCheck/FetchFinalStatus";

        public override bool IsEnabled => NEnv.IsOnlyNonStandardMortgageLoansEnabled;
        protected override MortgageLoanApplicationFinalCreditCheckStatusModel DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            return requestContext.Resolver().Resolve<IMortgageLoanApplicationCreditCheckService>().FetchApplicationFinalStatus(request.ApplicationNr);
        }

        public class Request
        {
            public string ApplicationNr { get; set; }
        }
    }
}