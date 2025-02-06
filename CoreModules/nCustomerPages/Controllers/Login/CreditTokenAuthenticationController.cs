using nCustomerPages.Code;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace nCustomerPages.Controllers
{
    [AllowAnonymous]
    [RoutePrefix("login")]
    public class CreditTokenAuthenticationController : Controller
    {
        public const string AuthType = "NtechCustomerPagesCreditToken";

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!NEnv.IsCreditTokenAuthenticationModeEnabled)
            {
                filterContext.Result = HttpNotFound();
            }
            base.OnActionExecuting(filterContext);
        }

        [Route("credittoken")]
        [PreventBackButton]
        public ActionResult LoginWithCreditToken(string token)
        {
            if (!NEnv.IsCreditTokenAuthenticationModeEnabled)
            {
                return HttpNotFound();
            }
            ViewBag.HideUserHeader = true;
            ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.GetEncoding("iso-8859-1").GetBytes(JsonConvert.SerializeObject(new
            {
                translation = BaseController.GetTranslationsShared(this.Url, this.Request)
            })));
            ViewBag.Token = token;
            return View();
        }

        [Route("apply-credittoken")]
        [HttpPost]
        public ActionResult ApplyLoginWithCreditToken(string token)
        {
            if (!NEnv.IsCreditTokenAuthenticationModeEnabled)
            {
                return HttpNotFound();
            }
            var c = new CreditClient();
            var result = c.TryLoginToCustomerPagesWithToken(token);
            if (result?.IsAllowedLogin ?? false)
            {
                var p = new LoginProvider();
                p.SignIn(this.HttpContext.GetOwinContext(), result.Customer.CustomerId, result.Customer.FirstName, false, AuthType, null, null);
                return RedirectToAction("Navigate", "CustomerPortal", new { targetName = CustomerNavigationTargetName.CreditOverview.ToString() });
            }
            else
            {
                return RedirectToAction("AccessDenied", "Common", new { isTokenExpired = result?.IsTokenExpired });
            }
        }
    }
}