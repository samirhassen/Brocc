using nPreCredit.Code.Services;
using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    public class MortgageLoanWorkListController : NController
    {
        [HttpPost]
        [Route("api/MortgageLoan/WorkList/FetchPage")]
        public ActionResult FetchPage(string currentBlockCode, int? pageNr, int? pageSize, bool? includeCurrentBlockCodeCounts, string separatedWorkList, bool? onlyNoHandlerAssignedApplications, int? assignedToHandlerUserId)
        {
            var s = DependancyInjection.Services.Resolve<IMortgageLoanWorkListService>();
            return Json2(s.GetWorkListPage(new MortgageLoanWorkListService.WorkListFilter
            {
                CurrentBlockCode = currentBlockCode,
                PageNr = pageNr,
                IncludeCurrentBlockCodeCounts = includeCurrentBlockCodeCounts,
                PageSize = pageSize,
                SeparatedWorkListName = separatedWorkList,
                OnlyNoHandlerAssignedApplications = onlyNoHandlerAssignedApplications.GetValueOrDefault(),
                AssignedToHandlerUserId = assignedToHandlerUserId
            }));
        }
    }
}