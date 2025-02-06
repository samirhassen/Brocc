using nCustomerPages.Code;
using NTech.Legacy.Module.Shared.Infrastructure;
using System;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace nCustomerPages.Controllers
{
    public class ProviderBasicAuthenticationAttribute : AuthenticationAttributeBase
    {
        private static Lazy<IpAddressRateLimiter> ipAddressRateLimiter = new Lazy<IpAddressRateLimiter>(() => new IpAddressRateLimiter());
        private static ApiKeyOrBearerTokenAuthHelper authHelper = new ApiKeyOrBearerTokenAuthHelper();

        public ProviderBasicAuthenticationAttribute()
        {

        }

        protected override void DoOnActionExecuting(ActionExecutingContext filterContext)
        {
            var callerIpAddress = filterContext.HttpContext?.GetOwinContext()?.Request?.RemoteIpAddress;

            if (!string.IsNullOrWhiteSpace(callerIpAddress) && ipAddressRateLimiter.Value.IsIpRateLimited(callerIpAddress))
            {
                filterContext.Result = new HttpStatusCodeResult(429, "Too many failed login attempts. You can try again in five minutes.");
                return;
            }

            var requestBody = RequestBody.CreateFromFromRequest(filterContext.HttpContext.Request, correlationId: Guid.NewGuid().ToString());

            if (NEnv.IsVerboseLoggingEnabled)
            {
                IncomingApiCallLog.SharedInstance.Log(requestBody.AsString(), contextPrefix: "providerApi");
            }
            var authHeader = requestBody.GetAuthorizationHeader();

            ApiKeyOrBearerTokenAuthHelper.AuthResult authResult = null;
            if (authHeader?.HeaderType == AuthorizationHeader.HeaderTypeCode.Basic)
            {
                authResult = authHelper.AuthenticateWithBasicAuth(authHeader.BasicAuthUserName, authHeader.BasicAuthPassword, callerIpAddress, true);
            }
            else if (authHeader?.HeaderType == AuthorizationHeader.HeaderTypeCode.Bearer)
            {
                authResult = authHelper.AuthenticateWithApiKey(authHeader.BearerToken, callerIpAddress, "ExternalCreditApplicationApi");
            }

            if (authResult != null)
                SetAuthResult(filterContext.HttpContext, authResult);
            else
                filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.Forbidden);
        }

        protected override string AttributeErrorLoggingName => "BasicAuthenticationAttribute";

        public static ApiKeyOrBearerTokenAuthHelper.AuthResult RequireAuthResult(HttpContextBase httpContext)
        {
            var model = httpContext.Items["ProviderBasicAuthenticationAttributeAuthResult"] as ApiKeyOrBearerTokenAuthHelper.AuthResult;
            if (model == null)
                throw new Exception("Missing auth result");
            return model;
        }

        private void SetAuthResult(HttpContextBase httpContext, ApiKeyOrBearerTokenAuthHelper.AuthResult authResult)
        {
            httpContext.Items["ProviderBasicAuthenticationAttributeAuthResult"] = authResult;
        }
    }
}