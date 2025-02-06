using nCustomerPages.Code;
using NTech.Legacy.Module.Shared.Infrastructure;
using Serilog;
using System;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace nCustomerPages.Controllers
{
    public abstract class AbstractProviderBasicAuthenticationAttribute : ActionFilterAttribute
    {
        private static Lazy<IpAddressRateLimiter> ipAddressRateLimiter = new Lazy<IpAddressRateLimiter>(() => new IpAddressRateLimiter());
        private static ApiKeyOrBearerTokenAuthHelper authHelper = new ApiKeyOrBearerTokenAuthHelper();

        public abstract string ProductName { get; }
        public abstract string ApiKeyScopeName { get; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                var callerIpAddress = filterContext.HttpContext?.GetOwinContext()?.Request?.RemoteIpAddress;

                if (!string.IsNullOrWhiteSpace(callerIpAddress) && ipAddressRateLimiter.Value.IsIpRateLimited(callerIpAddress))
                {
                    filterContext.Result = new HttpStatusCodeResult(429, "Too many failed login attempts. You can try again in five minutes.");
                    return;
                }

                var auth = filterContext.HttpContext.Request.Headers["Authorization"];
                var isValidHeader = AuthorizationHeader.TryParseHeader(auth, out var authHeader);

                ApiKeyOrBearerTokenAuthHelper.AuthResult authResult = null;
                if (isValidHeader && authHeader.HeaderType == AuthorizationHeader.HeaderTypeCode.Basic)
                {
                    authResult = authHelper.AuthenticateWithBasicAuth(authHeader.BasicAuthUserName, authHeader.BasicAuthPassword, callerIpAddress, true);
                }
                else if (isValidHeader && authHeader.HeaderType == AuthorizationHeader.HeaderTypeCode.Bearer)
                {
                    authResult = authHelper.AuthenticateWithApiKey(authHeader.BearerToken, callerIpAddress, ApiKeyScopeName);
                }
                if (authResult != null)
                    SetAuthResult(filterContext.HttpContext, authResult);
                else
                    filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            }
            catch (Exception ex)
            {
                NLog.Error(ex, $"Error in {ProductName}ProviderBasicAuthenticationAttribute");
                filterContext.HttpContext.Response.Clear();
                filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
                filterContext.HttpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                filterContext.Result = new HttpStatusCodeResult(System.Net.HttpStatusCode.InternalServerError, "Server error during login");
            }
        }

        public static ApiKeyOrBearerTokenAuthHelper.AuthResult RequireAuthResult(HttpContextBase httpContext)
        {
            var model = httpContext.Items["AbstractProviderBasicAuthenticationAttributeAuthResult"] as ApiKeyOrBearerTokenAuthHelper.AuthResult;
            if (model == null)
                throw new Exception("Missing auth result");
            return model;
        }

        private void SetAuthResult(HttpContextBase httpContext, ApiKeyOrBearerTokenAuthHelper.AuthResult authResult)
        {
            httpContext.Items["AbstractProviderBasicAuthenticationAttributeAuthResult"] = authResult;
        }

    }
}