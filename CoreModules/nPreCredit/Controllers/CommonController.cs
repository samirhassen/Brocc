using Newtonsoft.Json;
using NTech;
using NTech.Legacy.Module.Shared;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.Email;
using System;
using System.Web;
using System.Web.Mvc;

namespace nPreCredit.Controllers
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
                release = NTechSimpleSettings.GetValueFromClientResourceFile("CurrentReleaseMetadata.txt",
                    "releaseNumber", "No Current Release Info")
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
            if (User.Identity.IsAuthenticated)
            {
                HttpContext.GetOwinContext().Authentication.SignOut();
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
        public ActionResult Setup(bool verifyDb = true, bool clearCache = true)
        {
            ClockFactory.ResetTestClock();

            bool? isDbValid = null;
            if (verifyDb)
            {
                try
                {
                    using (var c = new PreCreditContext())
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
            if (clearCache)
            {
                CacheHandler.ClearAllCaches();
                if (!NEnv.IsProduction)
                {
                    InMemoryEmailTestService.ClearStoredEmails();
                }

                isCacheCleared = true;
            }

            return Json(new { isDbValid = isDbValid, isCacheCleared = isCacheCleared }, JsonRequestBehavior.AllowGet);
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

        [AllowAnonymous]
        [Route("translation")]
        public ActionResult Translation(string lang)
        {
            var t = Translations.FetchTranslation(lang);
            return t != null ? (ActionResult)Content(JsonConvert.SerializeObject(t), "application/json", System.Text.Encoding.UTF8) : HttpNotFound();
        }

        [HttpPost]
        [NTechApi]
        [Route("Api/Common/ReceiveEvent")]
        public ActionResult ApiReceiveEvent(string eventSource, string eventName, string eventData)
        {
            PreCreditEventCode c;
            if (Enum.TryParse(eventName, out c))
            {
                NTech.Services.Infrastructure.Eventing.NTechEventHandler.PublishEvent(c.ToString(), eventData);
            }

            return Json(new { });
        }
    }
}