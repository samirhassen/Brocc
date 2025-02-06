using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechAuthorizeCreditHigh]
    public class PreCollectionManagementController : NController
    {
        protected override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled && !NEnv.IsStandardUnsecuredLoansEnabled;

        [HttpGet]
        [Route("Ui/PreCollectionManagement/History")]
        public ActionResult History(int? testUserId)
        {
            SetInitialData(new
            {
                today = Clock.Today.ToString("yyyy-MM-dd"),
                getFilesPageUrl = Url.Action("GetHistoricalWorkListsPage", "ApiWorkList")
            });
            return View();
        }
    }
}