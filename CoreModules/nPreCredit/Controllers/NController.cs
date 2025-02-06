using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Clients;
using nPreCredit.Code.Services;
using NTech;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.PreCredit.Shared.Services.UlLegacy;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    public abstract class NController : Controller
    {
        protected const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        protected Lazy<NTechInitialDataHandler> initialDataHandler = new Lazy<NTechInitialDataHandler>(() => new NTechInitialDataHandler());

        protected void SetInitialData<T>(T data)
        {
            this.initialDataHandler.Value.SetInitialData(data, this);
        }

        protected virtual bool IsEnabled
        {
            get
            {
                return true;
            }
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!IsEnabled)
            {
                filterContext.Result = HttpNotFound();
                return;
            }
            base.OnActionExecuting(filterContext);
        }

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            this.initialDataHandler.Value.HandleOnActionExecuted(filterContext, this);
            base.OnActionExecuted(filterContext);
        }

        //TODO: Remove
        protected INTechCurrentUserMetadata NTechUser => DependancyInjection.Services.Resolve<INTechCurrentUserMetadata>();

        public int CurrentUserId => NTechUser.UserId;
        public string InformationMetadata => NTechUser.InformationMetadata;
        public IClock Clock => DependancyInjection.Services.Resolve<IClock>();
        //TODO: Remove end

        protected IDependencyResolver Service
        {
            get
            {
                return DependancyInjection.Services;
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
                dynamic m = new ExpandoObject();
                m.Title = title;
                m.Text = text;
                m.Link = link == null ? null : link.AbsoluteUri;
                s["infoMessageOnNextPageLoad"] = m;
            }
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
                return new UserClient().GetUserDisplayNamesByUserId();
            });
        }

        protected void AddCreditNrIfNeeded(string applicationNr, string stepName)
        {
            //Add a creditnumber if needed
            var ar = DependancyInjection.Services.Resolve<IPartialCreditApplicationModelRepository>();
            var appModel = ar.Get(
                applicationNr,
                applicationFields: new List<string> { "creditnr" });
            if (appModel.Application.Get("creditnr").StringValue.Optional == null)
            {
                var c = new CreditClient();
                var creditNr = c.NewCreditNumber();

                var repo = DependancyInjection.Services.Resolve<UpdateCreditApplicationRepository>();
                repo.UpdateApplication(applicationNr, new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest
                {
                    InformationMetadata = InformationMetadata,
                    StepName = stepName,
                    UpdatedByUserId = CurrentUserId,
                    Items = new List<UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem>
                    {
                        new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem
                        {
                            GroupName = "application",
                            Name = "creditnr",
                            IsSensitive = false,
                            Value = creditNr
                        }
                    }
                });
            }
        }

        protected bool UpdateAgreementStatus(string applicationNr)
        {
            var repo = DependancyInjection.Services.Resolve<IPartialCreditApplicationModelRepository>();
            string _;
            if (!repo.ExistsAll(applicationNr, out _, documentFields: new List<string> { "signed_initial_agreement_key" }))
            {
                return false;
            }

            using (var context = new PreCreditContext())
            {
                var h = context.CreditApplicationHeaders.Single(x => x.ApplicationNr == applicationNr);

                if (!h.IsActive || h.IsCancelled || h.IsFinalDecisionMade)
                    return false;

                if (h.AgreementStatus == "Accepted")
                    return false;

                h.AgreementStatus = "Accepted";
                h.ChangedById = CurrentUserId;
                h.ChangedDate = Clock.Now;
                context.SaveChanges();

                return true;
            }
        }

        protected bool UpdateCustomerCheckStatus(string applicationNr)
        {
            return UlLegacyAgreementSignatureService.UpdateCustomerCheckStatus(applicationNr,
                DependancyInjection.Services.Resolve<IPartialCreditApplicationModelRepository>(),
                DependancyInjection.Services.Resolve<IPreCreditContextFactoryService>(),
                DependancyInjection.Services.Resolve<NTech.Core.Module.Shared.Clients.ICustomerClient>());
        }

        protected T WithCache<T>(string key, Func<T> produce) where T : class
        {
            return NTechCache.WithCache(key, TimeSpan.FromMinutes(5), produce);
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

        protected static Uri AppendQueryStringParam(Uri uri, string name, string value)
        {
            var uriBuilder = new UriBuilder(uri);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query[name] = value;
            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri;
        }

        protected dynamic GetTranslations()
        {
            var userLang = System.Threading.Thread.CurrentThread?.CurrentUICulture?.TwoLetterISOLanguageName;
            return NTechCache.WithCache($"ntech.nprecredit.GetTranslations.{userLang ?? "en"}", TimeSpan.FromHours(1), () => GetTranslationsI(userLang));
        }

        private dynamic GetTranslationsI(string userLang)
        {
            var p = new ExpandoObject();

            var uiLanguage = "en";

            (p as IDictionary<string, object>)["en"] = Translations.FetchTranslation("en");

            if (userLang == "fi" || userLang == "sv")
            {
                uiLanguage = userLang;
                (p as IDictionary<string, object>)[userLang] = Translations.FetchTranslation(userLang);
            }

            return new
            {
                uiLanguage = uiLanguage,
                translateUrl = Url.Action("Translation", "Common"),
                translations = p
            };
        }

        protected ActionResult Json2(object data, int? customHttpStatusCode = null, string customStatusDescription = null)
        {
            return new JsonNetActionResult
            {
                Data = data,
                CustomHttpStatusCode = customHttpStatusCode,
                CustomStatusDescription = customStatusDescription
            };
        }

        protected ActionResult ServiceError(string errorMessage, string errorCode = null)
        {
            return NTech.Services.Infrastructure.NTechWs.NTechWebserviceMethod.ToFrameworkErrorActionResult(
                NTech.Services.Infrastructure.NTechWs.NTechWebserviceMethod.CreateErrorResponse(errorMessage, errorCode: errorCode));
        }
    }
}