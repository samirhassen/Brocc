using nCredit.Code.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NTech;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.Caching;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    public abstract class NController : Controller
    {
        protected const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private static readonly Lazy<MemoryCache> cache = new Lazy<MemoryCache>(() => MemoryCache.Default);

        protected Lazy<NTechInitialDataHandler> initialDataHandler = new Lazy<NTechInitialDataHandler>(() => new NTechInitialDataHandler());

        protected CreditContextExtended CreateCreditContext() => new CreditContextExtended(GetCurrentUserMetadata(), Clock);
        protected EncryptionService CreateEncryptionService() => new EncryptionService(NEnv.EncryptionKeys.CurrentKeyName, NEnv.EncryptionKeys.AsDictionary(), new CoreClock(), GetCurrentUserMetadata());

        protected void SetInitialData<T>(T data)
        {
            // TODO: If this works well, move the regularUserAccessToken logic to NTechInitialDataHandler. It exists to allow the core host to only support bearer tokens rather than the cookie hack we use here. This means that all api calls need the access token though.
            var user = GetCurrentUserMetadata();
            if (!user.IsSystemUser && user.AccessToken != null)
            {
                var temp = JObject.FromObject(data);
                temp.AddOrReplaceJsonProperty("regularUserAccessToken", new JValue(user.AccessToken), true);
                this.initialDataHandler.Value.SetInitialData(JsonConvert.DeserializeObject(temp.ToString()), this);
            }
            else
                this.initialDataHandler.Value.SetInitialData(data, this);
        }
        protected virtual bool IsEnabled
        {
            get
            {
                return true;
            }
        }

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (!IsEnabled)
            {
                filterContext.Result = HttpNotFound();
                return;
            }
            this.initialDataHandler.Value.HandleOnActionExecuted(filterContext, this);
            base.OnActionExecuted(filterContext);
        }

        public ClaimsIdentity Identity
        {
            get
            {
                return this.User.Identity as ClaimsIdentity;
            }
        }

        protected IClock Clock
        {
            get
            {
                return ClockFactory.SharedInstance;
            }
        }

        protected int CurrentUserId
        {
            get
            {
                return int.Parse(Identity.FindFirst("ntech.userid").Value);
            }
        }

        protected string CurrentUserAuthenticationLevel
        {
            get
            {
                return Identity.FindFirst("ntech.authenticationlevel").Value;
            }
        }

        public string InformationMetadata
        {
            get
            {
                return JsonConvert.SerializeObject(new
                {
                    providerUserId = CurrentUserId,
                    providerAuthenticationLevel = CurrentUserAuthenticationLevel,
                    isSigned = false
                });
            }
        }

        public INTechCurrentUserMetadata GetCurrentUserMetadata()
        {
            return new NTechCurrentUserMetadataImpl(this.Identity);
        }

        protected string GetUserDisplayNameByUserId(string userId)
        {
            var c = new Code.UserClient();
            var d = c.GetUserDisplayNamesByUserId();
            if (d.ContainsKey(userId))
                return d[userId];
            else
                return $"User {userId}";
        }

        private ControllerServiceFactory serviceFactory;

        protected ControllerServiceFactory Service
        {
            get
            {
                if (serviceFactory == null)
                {
                    serviceFactory = new ControllerServiceFactory(
                        this.GetUserDisplayNameByUserId,
                        new CoreClock(),
                        new Lazy<NTech.Services.Infrastructure.NTechWs.INTechWsUrlService>(() => Api.ApiHostController.ApiHost.Value),
                        new Lazy<INTechCurrentUserMetadata>(() => GetCurrentUserMetadata()));
                }
                return serviceFactory;
            }
        }

        protected T WithCache<T>(string key, Func<T> produce) where T : class
        {
            var val = cache.Value.Get(key) as T;
            if (val != null)
                return val;
            val = produce();
            cache.Value.Set(key, val, DateTimeOffset.Now.AddMinutes(5));
            return val;
        }

        private string GetClaim(string name)
        {
            return (this?.User?.Identity as System.Security.Claims.ClaimsIdentity)?.FindFirst(name)?.Value;
        }

        protected void GetProviderUserProperties(List<string> errors, out bool isProvider, out string providerName, out int userId, out string informationMetadata)
        {
            var isProviderClaim = GetClaim("ntech.isprovider");
            if (string.IsNullOrWhiteSpace(isProviderClaim))
            {
                errors.Add("Missing claim ntech.isprovider");
                isProvider = false;
            }
            else
            {
                isProvider = (isProviderClaim ?? "false") == "true";
            }

            providerName = GetClaim("ntech.providername");
            if (string.IsNullOrWhiteSpace(providerName))
            {
                errors.Add("Missing claim ntech.providername");
            }

            var userIdClaim = GetClaim("ntech.userid");
            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                userId = 0;
                errors.Add("Missing claim ntech.userid");
            }
            else if (!int.TryParse(userIdClaim, out userId))
            {
                errors.Add("Malformed claim ntech.userid");
            }

            var providerAuthenticationLevel = GetClaim("ntech.authenticationlevel");
            if (string.IsNullOrWhiteSpace(providerAuthenticationLevel))
                errors.Add("Missing claim ntech.authenticationlevel");

            if (errors.Count == 0)
            {
                informationMetadata = JsonConvert.SerializeObject(new
                {
                    providerUserId = userId,
                    providerAuthenticationLevel = providerAuthenticationLevel,
                    isSigned = false
                });
            }
            else
            {
                informationMetadata = null;
            }
        }

        protected void GetSystemUserProperties(List<string> errors, out bool isSystemUser, out int userId, out string informationMetadata)
        {
            var isSystemUserClaim = GetClaim("ntech.issystemuser");

            if (string.IsNullOrWhiteSpace(isSystemUserClaim))
            {
                errors.Add("Missing claim ntech.issystemuser");
                isSystemUser = false;
            }
            else
            {
                isSystemUser = (isSystemUserClaim ?? "false") == "true";
            }

            var userIdClaim = GetClaim("ntech.userid");
            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                userId = 0;
                errors.Add("Missing claim ntech.userid");
            }
            else if (!int.TryParse(userIdClaim, out userId))
            {
                errors.Add("Malformed claim ntech.userid");
            }

            var providerAuthenticationLevel = GetClaim("ntech.authenticationlevel");
            if (string.IsNullOrWhiteSpace(providerAuthenticationLevel))
                errors.Add("Missing claim ntech.authenticationlevel");

            if (errors.Count == 0)
            {
                informationMetadata = JsonConvert.SerializeObject(new
                {
                    providerUserId = userId,
                    providerAuthenticationLevel = providerAuthenticationLevel,
                    isSigned = false
                });
            }
            else
            {
                informationMetadata = null;
            }
        }

        /// <summary>
        /// Shown to user on the next page load
        /// </summary>
        /// <param name="title"></param>
        /// <param name="text"></param>
        protected void ShowInfoMessageOnNextPageLoad(string title, string text, Uri link = null)
        {
            var s = this?.HttpContext?.Session;
            if (s != null)
            {
                s["infoMessageOnNextPageLoad"] = CreateUserMessage(title, text, link);
            }
        }

        private dynamic CreateUserMessage(string title, string text, Uri link)
        {
            dynamic m = new ExpandoObject();
            m.Title = title;
            m.Text = text;
            m.Link = link == null ? null : link.AbsoluteUri;
            return m;
        }

        protected PartialViewResult CreatePartialViewUserMessage(string title, string text, Uri link = null)
        {
            ViewBag.Message = CreateUserMessage(title, text, link);
            return PartialView("UserMessage");
        }

        protected override ViewResult View(string viewName, string masterName, object model)
        {
            ConsumeInfoMessageOnNextPageLoad();
            return base.View(viewName, masterName, model);
        }

        protected override ViewResult View(IView view, object model)
        {
            ConsumeInfoMessageOnNextPageLoad();
            return base.View(view, model);
        }

        private void ConsumeInfoMessageOnNextPageLoad()
        {
            var s = this?.HttpContext?.Session;
            if (s != null)
            {
                ViewBag.Message = s["infoMessageOnNextPageLoad"];
                s["infoMessageOnNextPageLoad"] = null;
            }
        }

        protected ActionResult Json2(object data)
        {
            return new JsonNetActionResult
            {
                Data = data
            };
        }

        protected bool TryParseDataUrl(string dataUrl, out string mimeType, out byte[] binaryData)
        {
            var result = Regex.Match(dataUrl, @"data:(?<type>.+?);base64,(?<data>.+)");
            if (!result.Success)
            {
                mimeType = null;
                binaryData = null;
                return false;
            }
            else
            {
                mimeType = result.Groups["type"].Value.Trim();
                binaryData = Convert.FromBase64String(result.Groups["data"].Value.Trim());
                return true;
            }
        }
    }
}