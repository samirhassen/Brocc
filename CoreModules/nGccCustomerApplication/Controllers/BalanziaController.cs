using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Security;
using System.Linq;
using nGccCustomerApplication.Code;
using System.Dynamic;
using System.Threading;
using System.Web;
using System.Text;
using Serilog;
using IdentityModel.Client;
using NTech.Services.Infrastructure;

namespace nGccCustomerApplication.Controllers
{
    public class BalanziaController : NController
    {
        [Route("~/")]
        public ActionResult Application(int? amount, int? repaymentTimeInYears, string cc) //cc = campaign code
        {
            if(!NEnv.RedirectOldApplication)
            {
                return Redirect("/a/" + (this.Request.QueryString.Keys.Count > 0 ? $"?{this.Request.QueryString.ToString()}" : ""));
            }
            ViewBag.JsonInitialData = this.EncodeInitialData(new
            {
                isProduction = NEnv.IsProduction,
                translateUrl = Url.Action("Translation", "Common"),
                applyUrl = Url.Action("Apply"),
                failedUrl = Url.Action("Failed"),
                cancelUrl = Url.Action("Application"),
                amount = amount.HasValue ? amount.Value.ToString() : null,
                repaymentTimeInYears = repaymentTimeInYears.HasValue ? repaymentTimeInYears.Value.ToString() : null,
                svTranslations = Translations.FetchTranslation("sv"),
                fiTranslations = Translations.FetchTranslation("fi"),
                campaignCode = string.IsNullOrWhiteSpace(cc) ? null : cc.Trim().ToUpperInvariant()
            }).Replace("'", "\\'");
            return View();
        }

        [HttpPost]
        [Route("apply")]
        public ActionResult Apply(PreCreditClient.CreditApplicationRequest request)
        {
            if(request != null)
                request.RequestIpAddress = this.HttpContext?.GetOwinContext()?.Request?.RemoteIpAddress;
            return Json2(CreateApplication(request, this.Url));
        }

        public static CreateApplicationResult CreateApplication(PreCreditClient.CreditApplicationRequest request, UrlHelper h)
        {
            Func<CreateApplicationResult> fail = () =>
            {
                var failedUrl = NEnv.FailedUrl == null ? h.Action("Failed") : NEnv.FailedUrl.ToString();
                return new CreateApplicationResult
                {
                    isFailed = true,
                    failedUrl = failedUrl
                };
            };

            var c = new PreCreditClient();
            request.ProviderName = NEnv.SelfProviderName;
            var result = c.CreateCreditApplication(request);
            if (result.Item1)
            {
                string successUrl;
                if (NEnv.SuccessUrl != null)
                {
                    var b = new UriBuilder(NEnv.SuccessUrl);
                    b.Query = $"applicationNr={result.Item2.ApplicationNr}" + (string.IsNullOrWhiteSpace(request.UserLanguage) ? "" : "&lang=" + request.UserLanguage);
                    successUrl = b.ToString();
                }
                else
                {
                    successUrl = h.Action("Success", new { applicationNr = result.Item2.ApplicationNr, lang = request.UserLanguage });
                }

                return new CreateApplicationResult { isFailed = false, redirectToUrl = successUrl, applicationNr = result.Item2.ApplicationNr };
            }
            else
            {
                return fail();
            }
        }

        public class CreateApplicationResult
        {
            public bool isFailed { get; set; }
            public string redirectToUrl { get; set; }
            public string failedUrl { get; set; }
            public string applicationNr { get; set; }
        }

        [Route("thankyou")]
        public ActionResult Success(string applicationNr, string lang)
        {
            ViewBag.Language = lang ?? "fi";
            ViewBag.ApplicationNr = applicationNr;
            return View("Success");
        }

        [Route("failed")]
        public ActionResult Failed(string lang)
        {
            ViewBag.Language = lang ?? "fi";
            return View("Failed");
        }
    }
}
