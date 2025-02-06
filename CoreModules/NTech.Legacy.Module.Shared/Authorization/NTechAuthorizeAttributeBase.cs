using Serilog;
using System;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;

namespace NTech.Services.Infrastructure
{
    /*
     * Intended to do exactly the same thing as the built in one except that a system user can do the work of any role and it's possible to have it validate the access token
     */
    public abstract class NTechAuthorizeAttributeBase : FilterAttribute, IAuthorizationFilter
    {
        private readonly object _typeId = new object();

        public bool ValidateAccessToken { get; set; }

        public override object TypeId
        {
            get { return _typeId; }
        }

        private bool IsSystemUser(IPrincipal user)
        {
            return ((user as System.Security.Claims.ClaimsPrincipal)?.FindFirst("ntech.issystemuser")?.Value ?? "false") == "true";
        }

        // This method must be thread-safe since it is called by the thread-safe OnCacheAuthorization() method.
        protected virtual bool AuthorizeCore(HttpContextBase httpContext)
        {
            var isOk = AuthorizeCoreI(httpContext);
            if (ValidateAccessToken && isOk)
            {
                var ci = httpContext.User.Identity as System.Security.Claims.ClaimsIdentity;
                var expiresAt = ci?.FindFirst("expires_at")?.Value;
                if (expiresAt != null)
                {
                    DateTimeOffset d;
                    if (!DateTimeOffset.TryParse(expiresAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out d) || d < DateTimeOffset.Now)
                    {
                        Log.Information("Access token expired");
                        return false;
                    }
                }
            }
            return isOk;
        }

        protected abstract string[] GetUsers();
        protected abstract string[] GetRoles();

        private bool AuthorizeCoreI(HttpContextBase httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException("httpContext");
            }

            IPrincipal user = httpContext.User;
            if (!user.Identity.IsAuthenticated)
            {
                return false;
            }

            var _usersSplit = GetUsers();

            if (_usersSplit.Length > 0 && !_usersSplit.Contains(user.Identity.Name, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            var _rolesSplit = GetRoles();

            if (_rolesSplit.Length > 0 && (!IsSystemUser(user) && !_rolesSplit.Any(user.IsInRole)))
            {
                return false;
            }

            return true;
        }

        private void CacheValidateHandler(HttpContext context, object data, ref HttpValidationStatus validationStatus)
        {
            validationStatus = OnCacheAuthorization(new HttpContextWrapper(context));
        }

        public virtual void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException("filterContext");
            }

            if (OutputCacheAttribute.IsChildActionCacheActive(filterContext))
            {
                // If a child action cache block is active, we need to fail immediately, even if authorization
                // would have succeeded. The reason is that there's no way to hook a callback to rerun
                // authorization before the fragment is served from the cache, so we can't guarantee that this
                // filter will be re-run on subsequent requests.
                throw new InvalidOperationException();
            }

            bool skipAuthorization = filterContext.ActionDescriptor.IsDefined(typeof(AllowAnonymousAttribute), inherit: true)
                                     || filterContext.ActionDescriptor.ControllerDescriptor.IsDefined(typeof(AllowAnonymousAttribute), inherit: true);

            if (skipAuthorization)
            {
                return;
            }

            if (AuthorizeCore(filterContext.HttpContext))
            {
                // ** IMPORTANT **
                // Since we're performing authorization at the action level, the authorization code runs
                // after the output caching module. In the worst case this could allow an authorized user
                // to cause the page to be cached, then an unauthorized user would later be served the
                // cached page. We work around this by telling proxies not to cache the sensitive page,
                // then we hook our custom authorization code into the caching mechanism so that we have
                // the final say on whether a page should be served from the cache.

                HttpCachePolicyBase cachePolicy = filterContext.HttpContext.Response.Cache;
                cachePolicy.SetProxyMaxAge(new TimeSpan(0));
                cachePolicy.AddValidationCallback(CacheValidateHandler, null /* data */);
            }
            else
            {
                HandleUnauthorizedRequest(filterContext);
            }
        }

        protected virtual void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                // Returns HTTP 401 - see comment in HttpUnauthorizedResult.cs.
                filterContext.Result = new HttpUnauthorizedResult();
            }
            else
            {
                //Prevent redirect loop when authenticated but not autorized ... for instance when the uses lacks a role. 
                //Sending them back to the login will not help since they are logged in already.
                //NOTE: Could be made more user friendly by allowing a setting override per service with a custom redirect page requring no roles with for instance a list of what they need.
                filterContext.Result = new HttpStatusCodeResult(403);
            }
        }

        // This method must be thread-safe since it is called by the caching module.
        protected virtual HttpValidationStatus OnCacheAuthorization(HttpContextBase httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException("httpContext");
            }

            bool isAuthorized = AuthorizeCore(httpContext);
            return (isAuthorized) ? HttpValidationStatus.Valid : HttpValidationStatus.IgnoreThisRequest;
        }

        internal static string[] SplitString(string original)
        {
            if (String.IsNullOrEmpty(original))
            {
                return new string[0];
            }

            var split = from piece in original.Split(',')
                        let trimmed = piece.Trim()
                        where !String.IsNullOrEmpty(trimmed)
                        select trimmed;
            return split.ToArray();
        }
    }
}