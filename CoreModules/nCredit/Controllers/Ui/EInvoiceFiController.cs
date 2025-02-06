using nCredit.Code;
using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechAuthorizeCreditMiddle]
    public class EInvoiceFiController : NController
    {
        [HttpGet]
        [Route("Ui/EInvoiceFi/ImportIncomingMessageFile")]
        public ActionResult ImportIncomingMessageFile()
        {
            if (!NEnv.IsEInvoiceFiEnabled)
                return HttpNotFound();

            SetInitialData(new
            {
                importFileUrl = Url.Action("ImportIncomingMessageFile", "ApiEInvoiceFi"),
            });
            return View();
        }

        [HttpGet]
        [Route("Ui/EInvoiceFi/ErrorList")]
        public ActionResult ErrorList()
        {
            if (!NEnv.IsEInvoiceFiEnabled)
                return HttpNotFound();

            ViewBag.ManualMessageFileImportUrl = Url.Action("ImportIncomingMessageFile");

            SetInitialData(new
            {
                fetchErrorListActionItemsUrl = Url.Action("FetchErrorListActionItems", "ApiEInvoiceFi"),
                fetchActionDetailsUrl = Url.Action("FetchActionDetails", "ApiEInvoiceFi"),
                markActionAsHandledUrl = Url.Action("MarkActionAsHandled", "ApiEInvoiceFi"),
                userDisplayNameByUserId = (new UserClient()).GetUserDisplayNamesByUserId()
            });
            return View();
        }
    }
}