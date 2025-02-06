using nCustomerPages.Code;
using Newtonsoft.Json.Linq;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.IO;
using System.Web.Mvc;

namespace nCustomerPages.Controllers
{
    [HandleApiError]
    [MortgageLoanProviderBasicAuthentication]
    public abstract class MortgageLoanProviderApiBaseController : Controller
    {
        protected string CurrentProviderName => MortgageLoanProviderBasicAuthenticationAttribute.RequireAuthResult(this.HttpContext).ProviderName;
        private ProviderApiBaseHelper helper = new ProviderApiBaseHelper();

        private static readonly Lazy<RotatingLogFile> requestLog = new Lazy<RotatingLogFile>(
            () => new RotatingLogFile(Path.Combine(NEnv.LogFolder, "MortgageLoanProviderApi"), $"MortgageLoanProviderApiRequests", formatLogEntry: x => $"{x}{Environment.NewLine}--------------------------------------------------{Environment.NewLine}"));

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!NEnv.IsMortgageLoansEnabled || !NEnv.IsMortgageLoanProviderApiEnabled || NEnv.IsStandardMortgageLoansEnabled)
            {
                filterContext.Result = HttpNotFound();
            }

            base.OnActionExecuting(filterContext);
        }

        protected RawJsonActionResult ForwardApiRequest(string targetModule, string relativePath, JObject requestObject) =>
            helper.ForwardApiRequest(targetModule, relativePath, requestObject);

        protected RawJsonActionResult CreateError(System.Net.HttpStatusCode httpStatusCode, string errorCode, string errorMessage) =>
            helper.CreateError(httpStatusCode, errorCode, errorMessage);

        protected RawJsonActionResult WithRequestAsJObject(string methodPath, Func<JObject, RawJsonActionResult> f, string httpMethodName = "POST") =>
            helper.WithRequestAsJObject(Request, HttpContext, requestLog, CurrentProviderName, NEnv.IsMortgageLoanProviderApiLoggingEnabled, methodPath, f, httpMethodName);
    }
}