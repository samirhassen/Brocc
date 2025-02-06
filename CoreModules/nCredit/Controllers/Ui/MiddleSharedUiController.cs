using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nCredit.Controllers.Ui
{
    [NTechAuthorizeCreditMiddle]
    public class MiddleSharedUiController : SharedUiControllerBase
    {
        protected override bool IsEnabled => true;

        [Route("Ui/BookKeeping/EditRules")]
        public ActionResult EditBookKeepingRules()
        {
            return RenderComponent("bookkeeping-rules-edit", "Edit bookkeeping rules");
        }
    }
}