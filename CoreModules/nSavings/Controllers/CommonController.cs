using System;
using System.Globalization;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using nSavings.Code;
using nSavings.Code.Eventing;
using nSavings.DbModel;
using NTech;
using NTech.Legacy.Module.Shared;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.Eventing;

namespace nSavings.Controllers
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
                build = AssemblyName.GetAssemblyName(a.Location).Version.ToString(),
                release = NTechSimpleSettings.GetValueFromClientResourceFile("CurrentReleaseMetadata.txt",
                    "releaseNumber", "No Current Release Info")
            }, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        public ActionResult Error()
        {
            Response.StatusCode = 500;
            return View();
        }

        [HttpPost()]
        [NTechApi]
        [Route("Api/Common/ReceiveEvent")]
        public ActionResult ApiReceiveEvent(string eventSource, string eventName, string eventData)
        {
            if (Enum.TryParse(eventName, out SavingsEventCode c))
            {
                NTechEventHandler.PublishEvent(c.ToString(), eventData);
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

        [Route("TestAuth")]
        public ActionResult TestAuth()
        {
            return Content(this.User.Identity.Name);
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
        public ActionResult Setup(bool? verifyDb = null, bool? clearCache = null)
        {
            bool? isDbValid = null;
            if (verifyDb ?? true)
            {
                try
                {
                    using (var c = new SavingsContext())
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

        private bool DoTimeTravel(DateTime? date, bool? remove, string time)
        {
            if (NEnv.IsProduction)
                return false;

            DateTimeOffset? changeTo = null;
            var wasChanged = false;
            var now = DateTimeOffset.Now;
            if (remove.HasValue && remove.Value)
            {
                changeTo = now;
            }
            else if (date.HasValue)
            {
                var d = date.Value;
                changeTo = new DateTimeOffset(d.Year, d.Month, d.Day, now.Hour, now.Minute, now.Second, now.Offset);

                if (!string.IsNullOrWhiteSpace(time))
                {
                    d = DateTime.ParseExact($"{date.Value:yyyy-MM-dd} {time}", "yyyy-MM-dd HH:mm",
                        CultureInfo.InvariantCulture);
                    var n = changeTo.Value;
                    changeTo = new DateTimeOffset(n.Year, n.Month, n.Day, d.Hour, d.Minute, 0, n.Offset);
                }
            }

            if (changeTo.HasValue)
            {
                wasChanged = ClockFactory.TrySetApplicationDateAndTime(changeTo.Value);
            }

            return wasChanged;
        }

        [HttpPost, Route("Api/Common/TimeTravel")]
        public ActionResult ApiTimeTravel(DateTime? date, bool? remove, string time) //Time assumed to be HH:mm
        {
            if (NEnv.IsProduction)
                return HttpNotFound();

            var wasChanged = DoTimeTravel(date, remove, time);

            return Json(new { wasChanged = wasChanged });
        }
    }
}