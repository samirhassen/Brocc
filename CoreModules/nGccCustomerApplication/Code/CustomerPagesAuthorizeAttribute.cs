using Serilog;
using System;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;

namespace nGccCustomerApplication.Code
{
    public class CustomerPagesAuthorizeAttribute : FilterAttribute, IAuthorizationFilter
    {
        private readonly object _typeId = new object();

        private string _roles;
        private string[] _rolesSplit = new string[0];
        public bool ValidateAccessToken { get; set; } = true;

        public string Roles
        {
            get { return _roles ?? String.Empty; }
            set
            {
                _roles = value;
                _rolesSplit = SplitString(value);
            }
        }
        public bool IsApi { get; set; }

        public bool AllowEmptyRole { get; set; } = false;

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
            if(httpContext.Request.Url.AbsoluteUri.Contains("application-wrapper-link"))
                httpContext.Session["application-wrapper-token"] = httpContext.Request.Params["id"];
            
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

            if (!AllowEmptyRole && _rolesSplit.Length == 0 && !IsSystemUser(user))
            {
                //Special for customer pages. Only system users can access things without explicitly specifying a role except in explicity allowed places (like logout)
                return false;
            }

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
        public const string Force401HackItemName = "ntech_api_force_401";
        public const string Force403HackItemName = "ntech_api_force_403";

        protected virtual void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                // Returns HTTP 401 - see comment in HttpUnauthorizedResult.cs.
                filterContext.Result = new HttpUnauthorizedResult();
                if (IsApi)
                {
                    filterContext.RequestContext.HttpContext.Response.SuppressFormsAuthenticationRedirect = true;
                    filterContext.RequestContext.HttpContext.Items[Force401HackItemName] = "1";
                }
            }
            else
            {
                //Prevent redirect loop when authenticated but not autorized ... for instance when the uses lacks a role. 
                //Sending them back to the login will not help since they are logged in already.
                //NOTE: Could be made more user friendly by allowing a setting override per service with a custom redirect page requring no roles with for instance a list of what they need.
                filterContext.Result = new HttpStatusCodeResult(403);
                if (IsApi)
                {
                    filterContext.RequestContext.HttpContext.Response.SuppressFormsAuthenticationRedirect = true;
                    filterContext.RequestContext.HttpContext.Items[Force403HackItemName] = "1";
                }
            }
        }

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