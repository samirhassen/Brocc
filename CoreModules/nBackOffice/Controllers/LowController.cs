
using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nBackOffice.Controllers
{
    //Not in use any more
    [NTechAuthorizeCreditLow]
    public class LowController : NController
    {
        public ActionResult Index()
        {
            return RedirectToAction("NavMenu", "Secure");
        }
    }
}