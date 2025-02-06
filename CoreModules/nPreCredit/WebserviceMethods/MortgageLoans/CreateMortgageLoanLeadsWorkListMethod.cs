using nPreCredit.Code.Services.MortgageLoans;
using NTech.Services.Infrastructure.NTechWs;

namespace nPreCredit.WebserviceMethods
{
    public class CreateMortgageLoanLeadsWorkListMethod : TypedWebserviceMethod<CreateMortgageLoanLeadsWorkListMethod.Request, CreateMortgageLoanLeadsWorkListMethod.Response>
    {
        public override string Path => "MortgageLoan/Create-Leads-WorkList";

        public override bool IsEnabled => NEnv.IsOnlyNonStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var s = requestContext.Resolver().Resolve<IMortgageLoanLeadsWorkListService>();

            var (workListId, noLeadsMatchFilter) = s.TryCreateWorkList();

            return new Response
            {
                WorkListId = workListId,
                NoLeadsMatchFilter = noLeadsMatchFilter
            };
        }

        public class Request
        {

        }

        public class Response
        {
            public int? WorkListId { get; set; }
            public bool NoLeadsMatchFilter { get; set; }
        }
    }
}