using NTech.Legacy.Module.Shared;
using NTech.Services.Infrastructure;
using System.Web;
using System.Web.Mvc;

namespace nAudit.Controllers
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
                build = System.Reflection.AssemblyName.GetAssemblyName(a.Location).Version.ToString(),
                release = NTechSimpleSettings.GetValueFromClientResourceFile("CurrentReleaseMetadata.txt", "releaseNumber", "No Current Release Info")
            }, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        public ActionResult Error()
        {
            return View();
        }

        public ActionResult Logout()
        {
            this.HttpContext.GetOwinContext().Authentication.SignOut();
            return RedirectToAction("Loggedout");
        }

        [AllowAnonymous]
        public ActionResult Loggedout()
        {
            return View();
        }

        [Route("Setup")]
        [HttpGet()]
        public ActionResult Setup(bool? verifyDb = null, bool? clearCache = null)
        {
            Global.IsInitialized = false;
            try
            {
                bool? isDbValid = null;
                if (verifyDb ?? true)
                {

                    try
                    {
                        using (var c = new AuditContext())
                        {
                            c.Database.Initialize(true);
                        }
                        isDbValid = true;
                    }
                    catch
                    {
                        isDbValid = false;
                    }
                }

                bool? isCacheCleared = null;
                if (clearCache ?? true)
                {
                    CacheHandler.ClearAllCaches();
                    isCacheCleared = true;
                }

                return Json(new { isDbValid = isDbValid, isCacheCleared = isCacheCleared }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                Global.IsInitialized = true;
            }
        }
    }
}