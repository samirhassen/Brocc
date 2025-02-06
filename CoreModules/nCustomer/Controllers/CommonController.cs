using nCustomer.DbModel;
using NTech;
using NTech.Legacy.Module.Shared;
using NTech.Services.Infrastructure;
using System;
using System.Web;
using System.Web.Mvc;

namespace nCustomer.Controllers
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
            return View();
        }

        [AllowAnonymous]
        public ActionResult Error()
        {
            return View();
        }

        [Route("Setup")]
        [HttpGet()]
        public ActionResult Setup(bool? verifyDb = null, bool? clearCache = null)
        {
            ClockFactory.ResetTestClock();

            bool? isDbValid = null;
            if (verifyDb ?? true)
            {
                try
                {
                    CustomersContext.OnSetup();
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

        [HttpPost()]
        [Route("Api/Common/ReceiveEvent")]
        public ActionResult ApiReceiveEvent(string eventSource, string eventName, string eventData)
        {
            CustomerEventCode c;
            if (Enum.TryParse(eventName, out c))
            {
                NTech.Services.Infrastructure.Eventing.NTechEventHandler.PublishEvent(c.ToString(), eventData);
            }
            return Json(new { });
        }

        [Route("Set-TimeMachine-Time")]
        [HttpPost()]
        public ActionResult SetTimeMachine(DateTimeOffset? now)
        {
            if (NEnv.IsProduction)
                return HttpNotFound();

            var wasChanged = false;
            if (now.HasValue)
                wasChanged = ClockFactory.TrySetApplicationDateAndTime(now.Value);

            return Json(new { wasChanged });
        }
    }
}