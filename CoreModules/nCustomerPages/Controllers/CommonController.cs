using System;
using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Web.Mvc;
using nCustomerPages.Code;
using Newtonsoft.Json;
using NTech;
using NTech.Legacy.Module.Shared;
using NTech.Services.Infrastructure;

namespace nCustomerPages.Controllers;

[CustomerPagesAuthorize]
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
        ViewBag.ShowLogin =
            !(Session == null || (Session != null && Session["EidSignatureCustomerTarget"] == null));
        ViewBag.EidSignatureCustomerTarget = Session?["EidSignatureCustomerTarget"] != null
            ? Session["EidSignatureCustomerTarget"].ToString()
            : "";
        if (!NEnv.IsStandardEmbeddedCustomerPagesEnabled) return View();
        ViewBag.Message = "Du har loggat ut, saknar rättigheter eller har inga tjänster hos oss.";
        return View("EmbedddedCustomerPagesSimpleMessage");
    }

    [Route("overview")]
    [HttpGet]
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
        var reloginTargetName = GetClaim("ntech.claims.relogintargetname");
        var reloginTargetCustomData = GetClaim("ntech.claims.relogintargetcustomdata");

        var p = new LoginProvider();
        p.SignOut(HttpContext?.GetOwinContext());
        return RedirectToAction("Loggedout", new { reloginTargetName, reloginTargetCustomData });

        string GetClaim(string name) =>
            (User.Identity as ClaimsIdentity)?.FindFirst(name)?.Value?.NormalizeNullOrWhitespace();
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
            translation = BaseController.GetTranslationsShared(Url, Request)
        })));
        switch (reloginTargetName)
        {
            case nameof(CustomerNavigationTargetName.ApplicationsOverview):
                ViewBag.AllowRelogin = NEnv.IsDirectEidAuthenticationModeEnabled;
                ViewBag.ReloginUrl = Url.Action("LoginWithEid", "EidSignatureLogin",
                    new { targetName = reloginTargetName });
                return View("Loggedout_ApplicationsOverview");
            case nameof(CustomerNavigationTargetName.ContinueMortgageLoanApplication):
                ViewBag.AllowRelogin = NEnv.IsDirectEidAuthenticationModeEnabled;
                ViewBag.ReloginUrl = Url.Action("LoginWithEid", "EidSignatureLogin",
                    new { targetName = reloginTargetName, targetCustomData = reloginTargetCustomData });
                return View("Loggedout_ContinueMortgageLoanApplication");
            default:
                ViewBag.HideHeader = true;
                ViewBag.ShowLogin =
                    !(Session == null || (Session != null && Session["EidSignatureCustomerTarget"] == null));
                ViewBag.EidSignatureCustomerTarget = Session?["EidSignatureCustomerTarget"] != null
                    ? Session["EidSignatureCustomerTarget"].ToString()
                    : "";
                return View();
        }
    }

    [AllowAnonymous]
    [Route("translation")]
    public ActionResult Translation(string lang)
    {
        var t = Translations.FetchTranslation(lang);
        
        return t != null
            ? (ActionResult)Content(JsonConvert.SerializeObject(t), "application/json", Encoding.UTF8)
            : HttpNotFound();
    }

    [HttpGet]
    [Route("Setup")]
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

    private static bool DoTimeTravel(DateTime? date, bool? remove, string time)
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

    [HttpPost]
    [Route("Api/Common/TimeTravel")]
    public ActionResult ApiTimeTravel(DateTime? date, bool? remove, string time) //Time assumed to be HH:mm
    {
        if (NEnv.IsProduction)
            return HttpNotFound();

        var wasChanged = DoTimeTravel(date, remove, time);

        return Json(new { wasChanged });
    }

    [HttpPost]
    [Route("Set-TimeMachine-Time")]
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