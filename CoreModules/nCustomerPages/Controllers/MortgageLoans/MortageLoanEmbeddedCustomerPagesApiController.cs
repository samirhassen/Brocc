using nCustomerPages;
using Newtonsoft.Json.Linq;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace nTest.Controllers
{
    [NTechApi]
    [RoutePrefix(Prefix)]
    [CustomerPagesAuthorize(Roles = LoginProvider.EmbeddedMortageLoanCustomerPagesCustomer, IsApi = true)]
    public class MortageLoanEmbeddedCustomerPagesApiController : Controller
    {
        public const string Prefix = "api/embedded-ml-customerpages";

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!NEnv.IsMortgageLoansEnabled || !NEnv.IsEmbeddedMortageLoanCustomerPagesEnabled)
            {
                filterContext.Result = HttpNotFound();
            }
            base.OnActionExecuting(filterContext);
        }

        [Route("{*path}")]
        [HttpPost]
        public ActionResult Post()
        {
            var u = this.User.Identity as ClaimsIdentity;
            if (u == null)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            var customerIdRaw = u.FindFirst(LoginProvider.CustomerIdClaimName)?.Value;
            if (string.IsNullOrWhiteSpace(customerIdRaw))
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            var customerId = int.Parse(customerIdRaw);

            var path = this.RouteData.Values["path"] as string;

            var s = NEnv.ServiceRegistry;

            if (!Request.ContentType.Contains("application/json"))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid content type. Must be application/json");

            Request.InputStream.Position = 0;
            using (var r = new StreamReader(Request.InputStream, Request.ContentEncoding))
            {
                var requestString = r.ReadToEnd();

                var requestObject = JObject.Parse(requestString);

                requestObject.Add("CustomerId", customerId.ToString());

                var p = NHttp
                    .Begin(s.Internal.ServiceRootUri("nCredit"), NEnv.SystemUserBearerToken, TimeSpan.FromMinutes(5))
                    .PostJsonRaw($"Api/MortageLoan/CustomerPages/{path}", requestObject.ToString());

                if (p.IsSuccessStatusCode)
                {
                    return new RawJsonActionResult
                    {
                        JsonData = p.ParseAsRawJson()
                    };
                }
                else
                    return new HttpStatusCodeResult(p.StatusCode, "Failed");
            }
        }

        [Route("{*path}")]
        [HttpGet]
        public ActionResult Get()
        {
            var u = this.User.Identity as ClaimsIdentity;
            if (u == null)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            var customerIdRaw = u.FindFirst(LoginProvider.CustomerIdClaimName)?.Value;
            if (string.IsNullOrWhiteSpace(customerIdRaw))
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            var customerId = int.Parse(customerIdRaw);

            var path = this.RouteData.Values["path"] as string;

            var s = NEnv.ServiceRegistry;

            var query = HttpUtility.ParseQueryString(this.Request.Url.Query);
            query["CustomerId"] = customerId.ToString();

            var p = NHttp
                .Begin(s.Internal.ServiceRootUri("nCredit"), NEnv.SystemUserBearerToken, TimeSpan.FromMinutes(5))
                .Get($"Api/MortageLoan/CustomerPages/{path}" + (query.Count > 0 ? $"?{query.ToString()}" : ""));

            if (p.IsSuccessStatusCode)
            {
                var ms = new MemoryStream();
                p.DownloadFile(ms, out var contentType, out var filename);
                ms.Position = 0;
                return new FileStreamResult(ms, contentType) { FileDownloadName = filename };
            }
            else
                return new HttpStatusCodeResult(p.StatusCode, "Failed");
        }
    }
}