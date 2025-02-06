using nCustomerPages.Code;
using NTech.Core.Module.Shared.Clients;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

namespace nCustomerPages.Controllers
{
    public class ApiKeyAuthenticationAttribute : AuthenticationAttributeBase
    {
        private static Lazy<IUserClient> userClient = new Lazy<IUserClient>(() =>
            LegacyServiceClientFactory.CreateUserClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry));

        private static Lazy<IpAddressRateLimiter> ipAddressRateLimiter = new Lazy<IpAddressRateLimiter>(() => new IpAddressRateLimiter());

        public ApiKeyAuthenticationAttribute(string scope)
        {
            Scope = scope;
        }

        //Dont remove this and allow any as you could mount an attack on the non token based scopes if you could also create api keys
        private static readonly ISet<string> ScopeWhiteList = new HashSet<string>
        {
            "ExternalCustomerPagesApi", "ExternalCreditApplicationApi"
        };

        public string Scope { get; }

        private ApiKeyModel Authenticate(string apiKey, string callerIpAddress)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return null;
            if (string.IsNullOrWhiteSpace(Scope))
                return null;

            if (!ScopeWhiteList.Contains(Scope))
            {
                Log.Warning($"ApiKeyAuthenticationAttribute rejected for scope '{Scope}' since it's not whitelisted");
                return null;
            }

            var authResult = userClient.Value.AuthenticateWithApiKey(new ApiKeyAuthenticationRequest
            {
                AuthenticationScope = Scope,
                RawApiKey = apiKey,
                CallerIpAddress = callerIpAddress
            });

            ipAddressRateLimiter.Value.LogAuthenticationAttempt(callerIpAddress, authResult.IsAuthenticated);

            return authResult.IsAuthenticated ? authResult.AuthenticatedKeyModel : null;
        }

        protected override void DoOnActionExecuting(ActionExecutingContext filterContext)
        {
            var callerIpAddress = filterContext.HttpContext?.GetOwinContext()?.Request?.RemoteIpAddress;
            if (!string.IsNullOrWhiteSpace(callerIpAddress) && ipAddressRateLimiter.Value.IsIpRateLimited(callerIpAddress))
            {
                filterContext.Result = new HttpStatusCodeResult(429, "Too many failed attempts. You can try again in five minutes.");
                return;
            }

            var requestBody = RequestBody.CreateFromFromRequest(filterContext.HttpContext.Request, correlationId: Guid.NewGuid().ToString());

            if (NEnv.IsVerboseLoggingEnabled)
            {
                IncomingApiCallLog.SharedInstance.Log(requestBody.AsString(), contextPrefix: "apiKeyProtectedApi");
            }

            bool authorize = false;

            var authHeader = requestBody.GetAuthorizationHeader();
            if (authHeader?.HeaderType == AuthorizationHeader.HeaderTypeCode.Bearer)
            {
                var model = Authenticate(authHeader.BearerToken, callerIpAddress);

                if (model != null)
                {
                    filterContext.HttpContext.Items.Add("ApiKeyModel", model);
                    authorize = true;
                }
            }

            if (!authorize)
                filterContext.Result = new HttpStatusCodeResult(403);
        }

        protected override string AttributeErrorLoggingName => "ApiKeyAuthenticationAttribute";

        public static ApiKeyModel RequireAuthenticatedModel(HttpContextBase httpContext)
        {
            var model = httpContext.Items["ApiKeyModel"] as ApiKeyModel;
            if (model == null)
                throw new Exception("Missing api key model");
            return model;
        }
    }
}