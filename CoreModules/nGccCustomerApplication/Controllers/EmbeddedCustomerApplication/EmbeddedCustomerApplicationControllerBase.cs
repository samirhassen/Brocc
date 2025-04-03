
using Newtonsoft.Json.Linq;
using nGccCustomerApplication.Code;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Web.Mvc;

namespace nGccCustomerApplication.Controllers.EmbeddedCustomerApplication
{
    [CustomerPagesAuthorize(Roles = LoginProvider.EmbeddedCustomerPagesStandardRoleName)]
    public abstract class EmbeddedCustomerApplicationControllerBase : BaseController
    {

        protected virtual bool IsEnabled => true;
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!(NEnv.IsStandardUnsecuredLoansEnabled || NEnv.IsStandardMortgageLoansEnabled || NEnv.IsCustomerPagesKycQuestionsEnabled) || !IsEnabled)
            {
                filterContext.Result = HttpNotFound();
            }
            base.OnActionExecuting(filterContext);
        }

        protected static Lazy<NTechSelfRefreshingBearerToken> systemUserBearerToken = new Lazy<NTechSelfRefreshingBearerToken>(() =>
            NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry, NEnv.SystemUserUserNameAndPassword));

        protected ActionResult SendForwardApiCall(Func<JObject, ActionResult> editRequest, string moduleName, string localPath)
        {
            Func<string, string, ActionResult> error = (message, code) =>
                NTechWebserviceMethod.ToFrameworkErrorActionResult(NTechWebserviceMethod.CreateErrorResponse(message, errorCode: code, httpStatusCode: 400));

            if (!Request.ContentType.Contains("application/json"))
                return error("Invalid content type. Must be application/json", "invalidContentType");

            Request.InputStream.Position = 0;
            using (var r = new StreamReader(Request.InputStream, Request.ContentEncoding))
            {
                var request = JObject.Parse(r.ReadToEnd());
                var errorResult = editRequest(request);
                if (errorResult != null) return errorResult;

                return SendForwardApiCallDirect(request, moduleName, localPath);
            }
        }

        protected NHttp.NHttpCallResult SendPartialForwardApiCallDirect(JObject request, string moduleName, string localPath)
        {
            var s = NEnv.ServiceRegistry;
            if (!s.ContainsService(moduleName))
                return null; //NOTE: We can log more data here but dont leak it to the user

            var p = NHttp
                .Begin(s.Internal.ServiceRootUri(moduleName), systemUserBearerToken.Value.GetToken(), TimeSpan.FromMinutes(5))
                .PostJsonRaw(localPath, request.ToString(), headers: new Dictionary<string, string> { { "x-ntech-customerpages-forward", "true" } });
            return p;

        }

        protected ActionResult SendForwardApiCallDirect(JObject request, string moduleName, string localPath)
        {
            var p = SendPartialForwardApiCallDirect(request, moduleName, localPath);
            if (p == null)
                return HttpNotFound(); //NOTE: We can log more data here but dont leak it to the user

            if (p.IsSuccessStatusCode)
            {
                return new RawJsonActionResult
                {
                    JsonData = p.ParseAsRawJson()
                };
            }
            else
                return new HttpStatusCodeResult(p.StatusCode, p.ReasonPhrase);
        }

        protected bool TrySetOrReplaceCustomerIdFromLoggedInUser(JObject request)
        {
            var customerIdRaw = (User.Identity as ClaimsIdentity)?.FindFirst(LoginProvider.CustomerIdClaimName)?.Value;
            if (string.IsNullOrWhiteSpace(customerIdRaw))
                return false;
            var customerId = int.Parse(customerIdRaw);
            request.AddOrReplaceJsonProperty("CustomerId", new JValue(customerId), true);
            return true;
        }
    }
}