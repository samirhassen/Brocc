using nDataWarehouse.Code;
using Newtonsoft.Json;
using NTech.Legacy.Module.Shared;
using NTech.Services.Infrastructure;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

namespace nDataWarehouse.Controllers
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

        [AllowAnonymous]
        [Route("Logout")]
        public ActionResult Logout()
        {
            if (this.User.Identity.IsAuthenticated)
            {
                this.HttpContext.GetOwinContext().Authentication.SignOut();
            }
            return RedirectToAction("Loggedout");
        }

        [AllowAnonymous]
        [Route("Loggedout")]
        public ActionResult Loggedout()
        {
            if (this?.User?.Identity?.IsAuthenticated ?? false)
            {
                return RedirectToAction("Logout");
            }
            return View();
        }

        [Route("Setup")]
        [HttpGet()]
        public ActionResult Setup(bool? verifyDb = null, bool? clearCache = null)
        {
            bool? isDbValid = null;
            if (verifyDb ?? true)
            {
                try
                {
                    var support = new DwSupport();
                    support.SetupDb();
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

        private Dictionary<string, string> GetTranslations(string lang)
        {
            return null;
        }

        [AllowAnonymous]
        [Route("translation")]
        public ActionResult Translation(string lang)
        {
            var t = GetTranslations(lang);
            if (t != null)
            {
                return Content(JsonConvert.SerializeObject(t), "application/json", System.Text.Encoding.UTF8);
            }
            else
            {
                return HttpNotFound();
            }
        }
    }
}