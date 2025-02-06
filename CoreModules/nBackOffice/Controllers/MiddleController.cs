using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nBackOffice.Controllers
{
    [NTechAuthorizeCreditMiddle]
    public class MiddleController : NController
    {
        public ActionResult Index()
        {
            return RedirectToAction("NavMenu", "Secure");
        }
    }
}