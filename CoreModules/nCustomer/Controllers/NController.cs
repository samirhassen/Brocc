using nCustomer.Code;
using nCustomer.Code.Services;
using nCustomer.Code.Services.Kyc;
using Newtonsoft.Json;
using NTech;
using NTech.Core.Customer.Shared.Services;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.Caching;
using System.Security.Claims;
using System.Web.Mvc;

namespace nCustomer.Controllers
{
    public abstract class NController : Controller
    {
        protected IClock Clock
        {
            get
            {
                return ClockFactory.SharedInstance;
            }
        }

        protected Lazy<NTechInitialDataHandler> initialDataHandler = new Lazy<NTechInitialDataHandler>(() => new NTechInitialDataHandler());

        protected void SetInitialData<T>(T data)
        {
            this.initialDataHandler.Value.SetInitialData(data, this);
        }

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var wsException = filterContext.Exception as NTechCoreWebserviceException;
            if (wsException != null && wsException.IsUserFacing)
            {
                var result = new JsonNetActionResult
                {
                    Data = new
                    {
                        errorMessage = wsException?.Message ?? "generic",
                        errorCode = wsException?.ErrorCode ?? "generic"
                    }
                };
                result.CustomHttpStatusCode = wsException?.ErrorHttpStatusCode ?? 500;

                filterContext.Result = result;
                filterContext.ExceptionHandled = true;
            }

            this.initialDataHandler.Value.HandleOnActionExecuted(filterContext, this);
            base.OnActionExecuted(filterContext);
        }

        protected static Lazy<KycScreeningProviderServiceFactory> kycServiceFactory = new Lazy<KycScreeningProviderServiceFactory>(() => new KycScreeningProviderServiceFactory());

        private ControllerServiceFactory serviceFactory;

        protected ControllerServiceFactory Service
        {
            get
            {
                if (serviceFactory == null)
                {
                    serviceFactory = new ControllerServiceFactory(
                        this.Url,
                        this.GetUserDisplayNameByUserId,
                        CoreClock.SharedInstance,
                        this.GetUserDisplayNamesByUserId,
                        kycServiceFactory,
                        this.GetCurrentUserMetadata());
                }
                return serviceFactory;
            }
        }

        public NtechCurrentUserMetadata GetCurrentUserMetadata()
        {
            return new NtechCurrentUserMetadata
            {
                InformationMetadata = this.InformationMetadata,
                UserId = this.CurrentUserId,
                CoreUser = new NTechCurrentUserMetadataImpl(this.Identity)
            };
        }

        protected dynamic GetTranslations()
        {
            var userLang = System.Threading.Thread.CurrentThread?.CurrentUICulture?.TwoLetterISOLanguageName ?? "en";
            var service = new UiTranslationService(() => NEnv.IsTranslationCacheDisabled, NEnv.ClientCfgCore);

            var t = service.GetTranslations(userLang);

            return new
            {
                uiLanguage = t.UiLanguage,
                translateUrl = Url.Action("Translation", "Common"),
                translations = t.Translations
            };
        }

        private static readonly Lazy<MemoryCache> cache = new Lazy<MemoryCache>(() => MemoryCache.Default);

        public ClaimsIdentity Identity
        {
            get
            {
                return this.User.Identity as ClaimsIdentity;
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

        private class GetUserDisplayNamesByUserIdResult
        {
            public string UserId { get; set; }
            public string DisplayName { get; set; }
        }

        protected string GetUserDisplayNameByUserId(string userId)
        {
            var d = GetUserDisplayNamesByUserId();
            if (d.ContainsKey(userId))
                return d[userId];
            else
                return $"User {userId}";
        }

        private Dictionary<string, string> GetUserDisplayNamesByUserId()
        {
            return WithCache("nPreCredit.Controllers.NController.GetUserDisplayNamesByUserId", () =>
            {
                return NHttp
                    .Begin(new Uri(NEnv.ServiceRegistry.Internal["nUser"]), NHttp.GetCurrentAccessToken())
                    .PostJson("User/GetAllDisplayNamesAndUserIds", new { })
                    .ParseJsonAs<GetUserDisplayNamesByUserIdResult[]>()
                    .ToDictionary(x => x.UserId, x => x.DisplayName);
            });
        }

        protected T WithCache<T>(string key, Func<T> produce) where T : class
        {
            var val = cache.Value.Get(key) as T;
            if (val != null)
                return val;
            val = produce();
            cache.Value.Set(key, val, DateTimeOffset.Now.AddMinutes(5)); //NOTE: Intentionally not using the timemachine as the cache uses the system clock internally
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

        protected CustomerWriteRepository CreateWriteRepo(DbModel.CustomersContext db)
        {
            string informationMetadata = InformationMetadata;
            int userId;


            if (Int32.TryParse((this?.User?.Identity as System.Security.Claims.ClaimsIdentity)?.FindFirst("ntech.userid")?.Value, out userId))
            {
                return new CustomerWriteRepository(db,
                    GetCurrentUserMetadata().CoreUser,
                    CoreClock.SharedInstance, Service.EncryptionService, NEnv.ClientCfgCore);
            }
            else
            {
                throw new Exception("Could not create Customer Repository, no user found.");
            }
        }

        protected CustomerSearchRepository CreateSearchRepo(DbModel.CustomersContext db) =>
            new CustomerSearchRepository(db, Service.EncryptionService, NEnv.ClientCfgCore);


        protected ActionResult Json2(object data)
        {
            return new JsonNetActionResult
            {
                Data = data
            };
        }
    }
}