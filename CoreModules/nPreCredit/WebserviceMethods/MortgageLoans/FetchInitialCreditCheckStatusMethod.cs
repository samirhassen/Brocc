using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;

namespace nPreCredit.WebserviceMethods.MortgageLoans
{
    public class FetchInitialCreditCheckStatusMethod : TypedWebserviceMethod<FetchInitialCreditCheckStatusMethod.Request, MortgageLoanApplicationInitialCreditCheckStatusModel>
    {
        public override string Path => "MortgageLoan/CreditCheck/FetchInitialStatus";

        public override bool IsEnabled => NEnv.IsOnlyNonStandardMortgageLoansEnabled;

        protected override MortgageLoanApplicationInitialCreditCheckStatusModel DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            return requestContext.Resolver().Resolve<IMortgageLoanApplicationCreditCheckService>().FetchApplicationInitialStatus(request.ApplicationNr);
        }

        public class Request
        {
            public string ApplicationNr { get; set; }
        }
    }
}