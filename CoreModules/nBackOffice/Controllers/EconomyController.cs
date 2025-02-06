using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nBackOffice.Controllers
{
    //Not in use any more
    [NTechAuthorizeCreditEconomy()]
    public class EconomyController : NController
    {
        public ActionResult Index()
        {
            return RedirectToAction("NavMenu", "Secure");
        }
    }
}