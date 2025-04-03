using nCustomerPages.Code;
using Newtonsoft.Json;
using NTech;
using NTech.Legacy.Module.Shared;
using NTech.Services.Infrastructure;
using System;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace nCustomerPages.Controllers
{
    [CustomerPagesAuthorize()]
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
            //BEWARE: Do not put translation logic or similar here. If an error occurs here ... inception.
            return View();
        }

        [AllowAnonymous]
        [Route("access-denied")]
        public ActionResult AccessDenied(bool? isTokenExpired)
        {
            ViewBag.HideHeader = true;
            ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
            {
                translation = BaseController.GetTranslationsShared(this.Url, this.Request)
            })));
            ViewBag.IsTokenExpired = isTokenExpired ?? false;
            //TODO; Jatin
            Session["EidSignatureCustomerTarget"]=  "http://localhost/ncustomerpages/login/eid/test333/return";
            ViewBag.ShowLogin = !(Session == null || (Session != null && Session["EidSignatureCustomerTarget"] == null));
            ViewBag.EidSignatureCustomerTarget =  Session != null && Session["EidSignatureCustomerTarget"] != null ? Session["EidSignatureCustomerTarget"].ToString() : "";
            if (NEnv.IsStandardEmbeddedCustomerPagesEnabled)
            {
                ViewBag.Message = "Du har loggat ut, saknar rättigheter eller har inga tjänster hos oss.";
                return View("EmbedddedCustomerPagesSimpleMessage");
            }
            else
            {
                return View();
            }
        }

        [Route("overview")]
        [HttpGet()]
        [CustomerPagesAuthorize(AllowEmptyRole = true)]
        [PreventBackButton]
        public ActionResult ProductsOverview()
        {
            ViewBag.ShowLogoutButton = true;
            ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
            {
                translation = BaseController.GetTranslationsShared(this.Url, this.Request)
            })));
            return View();
        }

        [Route("logout")]
        [CustomerPagesAuthorize(AllowEmptyRole = true)]
        public ActionResult Logout()
        {
            string GetClaim(string name) =>
                (User.Identity as ClaimsIdentity)?.FindFirst(name)?.Value?.NormalizeNullOrWhitespace();

            var reloginTargetName = GetClaim("ntech.claims.relogintargetname");
            var reloginTargetCustomData = GetClaim("ntech.claims.relogintargetcustomdata");

            var p = new LoginProvider();
            p.SignOut(this.HttpContext?.GetOwinContext());
            return RedirectToAction("Loggedout", new { reloginTargetName, reloginTargetCustomData });
        }

        [Route("")]
        [CustomerPagesAuthorize(AllowEmptyRole = true)]
        public ActionResult Default()
        {
            return RedirectToAction("ProductsOverview", "Common");
        }

        [Route("loggedout")]
        [AllowAnonymous]
        public ActionResult Loggedout(string reloginTargetName, string reloginTargetCustomData)
        {
            ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
            {
                translation = BaseController.GetTranslationsShared(this.Url, this.Request)
            })));
            if (reloginTargetName == CustomerNavigationTargetName.ApplicationsOverview.ToString())
            {
                ViewBag.AllowRelogin = NEnv.IsDirectEidAuthenticationModeEnabled;
                ViewBag.ReloginUrl = Url.Action("LoginWithEid", "EidSignatureLogin", new { targetName = reloginTargetName });
                return View("Loggedout_ApplicationsOverview");
            }
            else if (reloginTargetName == CustomerNavigationTargetName.ContinueMortgageLoanApplication.ToString())
            {
                ViewBag.AllowRelogin = NEnv.IsDirectEidAuthenticationModeEnabled;
                ViewBag.ReloginUrl = Url.Action("LoginWithEid", "EidSignatureLogin", new { targetName = reloginTargetName, targetCustomData = reloginTargetCustomData });
                return View("Loggedout_ContinueMortgageLoanApplication");
            }
            else
            {
                ViewBag.HideHeader = true;
                ViewBag.ShowLogin = !(Session == null || (Session != null && Session["EidSignatureCustomerTarget"] == null));
                ViewBag.EidSignatureCustomerTarget = Session != null && Session["EidSignatureCustomerTarget"] != null ? Session["EidSignatureCustomerTarget"].ToString() : "";
                return View();
            }
        }

        [AllowAnonymous]
        [Route("translation")]
        public ActionResult Translation(string lang)
        {
            var t = Translations.FetchTranslation(lang);
            if (t != null)
            {
                return Content(JsonConvert.SerializeObject(t), "application/json", System.Text.Encoding.UTF8);
            }
            else
            {
                return HttpNotFound();
            }
        }

        [Route("Setup")]
        [HttpGet()]
        public ActionResult Setup(bool? verifyDb = null, bool? clearCache = null)
        {
            if (NEnv.IsProduction)
                return HttpNotFound();

            bool? isCacheCleared = null;
            if (clearCache ?? true)
            {
                CacheHandler.ClearAllCaches();
                isCacheCleared = true;
            }

            return Json(new { isCacheCleared = isCacheCleared }, JsonRequestBehavior.AllowGet);
        }

        private bool DoTimeTravel(DateTime? date, bool? remove, string time)
        {
            if (NEnv.IsProduction)
                return false;

            DateTimeOffset? changeTo = null;
            bool wasChanged = false;
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
                    d = DateTime.ParseExact($"{date.Value.ToString("yyyy-MM-dd")} {time}", "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
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

        [Route("Api/Common/TimeTravel")]
        [HttpPost()]
        public ActionResult ApiTimeTravel(DateTime? date, bool? remove, string time) //Time assumed to be HH:mm
        {
            if (NEnv.IsProduction)
                return HttpNotFound();

            var wasChanged = DoTimeTravel(date, remove, time);

            return Json(new { wasChanged });
        }

        [Route("Set-TimeMachine-Time")]
        [HttpPost()]
        [AllowAnonymous] //Ok since only active in test
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