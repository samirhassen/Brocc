using System.Reflection;
using System.Security.Principal;
using System.Web.Mvc;
using NTech.Services.Infrastructure;

namespace nWindowsAuthIdentityServer.Controllers
{
    public class CommonController : Controller
    {
        [AllowAnonymous]
        public ActionResult Hb()
        {
            var a = Assembly.GetExecutingAssembly();
            return Json(new
            {
                status = "ok",
                name = a.GetName().Name,
                build = AssemblyName.GetAssemblyName(a.Location).Version.ToString()
            }, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        public ActionResult Error()
        {
            return View();
        }

        [NTechAuthorize]
        public ActionResult Debug()
        {
            if (!NEnv.IsDebugPageEnabled)
                return HttpNotFound();

            var p = this.User as WindowsPrincipal;
            return View(p);
        }
    }
}