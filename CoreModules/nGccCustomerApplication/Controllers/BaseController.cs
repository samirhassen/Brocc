using Newtonsoft.Json;
using NTech;
using NTech.Banking.Shared.BankAccounts.Fi;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace nGccCustomerApplication.Controllers
{
    public class BaseController : AnonymousBaseController
    {
        // GET: Base
         protected System.Security.Claims.ClaimsIdentity CustomerUser
        {
            get
            {
                return User?.Identity as System.Security.Claims.ClaimsIdentity;
            }
        }

        protected int CustomerId
        {
            get
            {
                return Code.LoginProvider.GetCustomerId(CustomerUser).Value;
            }
        }

        protected bool IsStrongIdentity
        {
            get
            {
                return CustomerUser?.FindFirst("ntech.claims.isstrongidentity")?.Value == "true";
            }
        }

        protected string InferBankNameFromIbanFi(IBANFi iban)
        {
            try
            {
                return NEnv.IBANToBICTranslatorInstance.InferBankName(iban) ?? "Unknown";
            }
            catch
            {
                //If a new bank gets added we dont want the ui to crash. Better to just not show it.
                return "Unknown";
            }
        }

        public NTech.Banking.CivicRegNumbers.ICivicRegNumber CustomerCivicRegNumber
        {
            get
            {
                var v = CustomerUser?.FindFirst("ntech.claims.civicregnr")?.Value;
                if (string.IsNullOrWhiteSpace(v))
                    return null;
                else
                    return NEnv.BaseCivicRegNumberParser.Parse(v);
            }
        }

        public static Dictionary<string, string> GetTranslationsSharedDict(UrlHelper h, HttpRequestBase request, Action<string> observeUserLanguage = null)
        {
            return GetTranslationsSharedI(h, request, "ntech.ncustomerpages.GetTranslationsSharedDict", x => Code.Translations.GetTranslationTable(x), observeUserLanguage: observeUserLanguage);
        }

        public static dynamic GetTranslationsShared(UrlHelper h, HttpRequestBase request, Action<string> observeUserLanguage = null)
        {
            return GetTranslationsSharedI(h, request, "ntech.ncustomerpages.GetTranslations", x => GetTranslationsI(x, h), observeUserLanguage: observeUserLanguage);
        }

        public static string GetUserLanguage(HttpRequestBase request)
        {
            var defaultLanguage = GetDefaultLanguage();

            var userLang = request?.Cookies[LanguageOverrideCookieName]?.Value ?? System.Threading.Thread.CurrentThread?.CurrentUICulture?.TwoLetterISOLanguageName ?? defaultLanguage;
            if (!IsSupportedLanguage(userLang))
                userLang = defaultLanguage;

            return userLang;
        }

        private static T GetTranslationsSharedI<T>(UrlHelper h, HttpRequestBase request, string context, Func<string, T> fromUserLanguage, Action<string> observeUserLanguage = null) where T : class
        {
            var userLang = GetUserLanguage(request);

            observeUserLanguage?.Invoke(userLang);

            if (NEnv.IsTranslationCacheDisabled)
                return fromUserLanguage(userLang);
            else
                return NTech.Services.Infrastructure.NTechCache.WithCache($"{context}.{userLang}", TimeSpan.FromHours(1), () => fromUserLanguage(userLang));
        }

        private static bool IsSupportedLanguage(string language)
        {
            var country = NEnv.ClientCfg.Country.BaseCountry;
            if (country == "FI")
                return language.IsOneOf("sv", "fi");
            else if (country == "SE")
                return language.IsOneOf("sv");
            else
                return false; //Always use default
        }
        private static string GetDefaultLanguage()
        {
            var country = NEnv.ClientCfg.Country.BaseCountry;
            if (country == "SE")
                return "sv";
            else if (country == "FI")
                return "fi";
            else
                return "fi";
        }

        private const string LanguageOverrideCookieName = "ntechcustomerpageslangv1";

        protected dynamic GetTranslations(Action<string> observeUserLanguage = null)
        {
            return GetTranslationsShared(this.Url, this.Request, observeUserLanguage: observeUserLanguage);
        }

        private static dynamic GetTranslationsI(string userLang, UrlHelper h)
        {
            var p = new System.Dynamic.ExpandoObject();

            var uiLanguage = "fi";

            (p as IDictionary<string, object>)[uiLanguage] = Code.Translations.FetchTranslation(uiLanguage);
            if (userLang == "sv")
            {
                uiLanguage = userLang;
                (p as IDictionary<string, object>)[uiLanguage] = Code.Translations.FetchTranslation(uiLanguage);
            }

            return new
            {
                uiLanguage = uiLanguage,
                translateUrl = h.Action("Translation", "Common"),
                translations = p
            };
        }

        protected Uri GetExternalLink(string action, string controller, object routeValues)
        {
            var u = Url.Action(action, controller, routeValues);
            if (u == null)
                throw new NotImplementedException();
            return new Uri(Url.Action(action, controller, routeValues, this.Request.Url.Scheme));
        }

        protected void LogUserAction(string actionName)
        {
            Code.LoginProvider.LogUserAction(actionName, this.User?.Identity as System.Security.Claims.ClaimsIdentity);
        }

    }

    public abstract class AnonymousBaseController : Controller
    {
        protected IClock Clock
        {
            get
            {
                return ClockFactory.SharedInstance;
            }
        }

        protected ActionResult Json2(object data)
        {
            return new JsonNetResult
            {
                Data = data
            };
        }

        public class JsonNetResult : ActionResult
        {
            public Encoding ContentEncoding { get; set; }
            public string ContentType { get; set; }
            public object Data { get; set; }

            public JsonSerializerSettings SerializerSettings { get; set; }
            public Formatting Formatting { get; set; }

            public JsonNetResult()
            {
                SerializerSettings = new JsonSerializerSettings();
            }

            public override void ExecuteResult(ControllerContext context)
            {
                if (context == null)
                    throw new ArgumentNullException("context");

                HttpResponseBase response = context.HttpContext.Response;

                response.ContentType = !string.IsNullOrEmpty(ContentType)
                  ? ContentType
                  : "application/json";

                if (ContentEncoding != null)
                    response.ContentEncoding = ContentEncoding;

                if (Data != null)
                {
                    JsonTextWriter writer = new JsonTextWriter(response.Output) { Formatting = Formatting };

                    JsonSerializer serializer = JsonSerializer.Create(SerializerSettings);
                    serializer.Serialize(writer, Data);

                    writer.Flush();
                }
            }
        }
    }
}