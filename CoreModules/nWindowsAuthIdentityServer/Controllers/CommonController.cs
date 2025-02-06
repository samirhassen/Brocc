using NTech.Services.Infrastructure;
using System.Security.Principal;
using System.Web.Mvc;

namespace nWindowsAuthIdentityServer.Controllers
{
    public class CommonController : Controller
    {
        [AllowAnonymous]
        public ActionResult Hb()
        {
            var a = System.Reflection.Assembly.GetExecutingAssembly();
            return Json(new
            {
                status = "ok",
                name = a.GetName().Name,
                build = System.Reflection.AssemblyName.GetAssemblyName(a.Location).Version.ToString()
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