using nCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;

namespace nCredit.WebserviceMethods
{
    public class GetReferenceInterestRateChangesPageMethod : TypedWebserviceMethod<GetReferenceInterestRateChangesPageMethod.Request, ReferenceInterestChangePage>
    {
        public override string Path => "Credit/GetReferenceInterestRateChangesPage";

        protected override ReferenceInterestChangePage DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            var s = requestContext.Service().ReferenceInterestChange;
            return s.GetReferenceInterestRateChangesPage(request.pageSize ?? 20, request.pageNr ?? 0);
        }

        public class Request
        {
            public int? pageSize { get; set; }
            public int? pageNr { get; set; }
        }
    }
}