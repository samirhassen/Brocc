using System.Web.Mvc;

namespace nBackOffice.Controllers
{
    //Not in use any more.
    public class UserGatewayController : Controller
    {
        public ActionResult GotoStartPage()
        {
            return RedirectToAction("NavMenu", "Secure");
        }
    }
}