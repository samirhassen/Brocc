using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using NTech;
using NTech.Banking.CivicRegNumbers;
using NTech.Banking.Shared.BankAccounts.Fi;
using NTech.Services.Infrastructure;

namespace nCustomerPages.Controllers
{
    public abstract class BaseController : AnonymousBaseController
    {
        protected ClaimsIdentity CustomerUser => User?.Identity as ClaimsIdentity;

        protected int CustomerId => LoginProvider.GetCustomerId(CustomerUser).Value;

        protected bool IsStrongIdentity => CustomerUser?.FindFirst("ntech.claims.isstrongidentity")?.Value == "true";

        protected static string InferBankNameFromIbanFi(IBANFi iban)
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

        public ICivicRegNumber CustomerCivicRegNumber
        {
            get
            {
                var v = CustomerUser?.FindFirst("ntech.claims.civicregnr")?.Value;
                return string.IsNullOrWhiteSpace(v) ? null : NEnv.BaseCivicRegNumberParser.Parse(v);
            }
        }

        public static Dictionary<string, string> GetTranslationsSharedDict(UrlHelper h, HttpRequestBase request,
            Action<string> observeUserLanguage = null)
        {
            return GetTranslationsSharedI(h, request, "ntech.ncustomerpages.GetTranslationsSharedDict",
                Translations.GetTranslationTable, observeUserLanguage: observeUserLanguage);
        }

        public static dynamic GetTranslationsShared(UrlHelper h, HttpRequestBase request,
            Action<string> observeUserLanguage = null)
        {
            return GetTranslationsSharedI(h, request, "ntech.ncustomerpages.GetTranslations",
                x => GetTranslationsI(x, h), observeUserLanguage: observeUserLanguage);
        }

        public static string GetUserLanguage(HttpRequestBase request)
        {
            var defaultLanguage = GetDefaultLanguage();

            var userLang = request?.Cookies[LanguageOverrideCookieName]?.Value ??
                           Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName ??
                           defaultLanguage;
            if (!IsSupportedLanguage(userLang))
                userLang = defaultLanguage;

            return userLang;
        }

        private static T GetTranslationsSharedI<T>(UrlHelper h, HttpRequestBase request, string context,
            Func<string, T> fromUserLanguage, Action<string> observeUserLanguage = null) where T : class
        {
            var userLang = GetUserLanguage(request);

            observeUserLanguage?.Invoke(userLang);

            if (NEnv.IsTranslationCacheDisabled)
                return fromUserLanguage(userLang);
            return NTechCache.WithCache($"{context}.{userLang}", TimeSpan.FromHours(1),
                () => fromUserLanguage(userLang));
        }

        private static bool IsSupportedLanguage(string language)
        {
            var country = NEnv.ClientCfg.Country.BaseCountry;
            return country switch
            {
                "FI" => language.IsOneOf("sv", "fi"),
                "SE" => language.IsOneOf("sv"),
                _ => false
            };
        }

        private static string GetDefaultLanguage()
        {
            var country = NEnv.ClientCfg.Country.BaseCountry;
            return country switch
            {
                "SE" => "sv",
                "FI" => "fi",
                _ => "fi"
            };
        }

        private const string LanguageOverrideCookieName = "ntechcustomerpageslangv1";

        protected dynamic GetTranslations(Action<string> observeUserLanguage = null)
        {
            return GetTranslationsShared(this.Url, this.Request, observeUserLanguage: observeUserLanguage);
        }

        private static dynamic GetTranslationsI(string userLang, UrlHelper h)
        {
            var p = new ExpandoObject();

            var uiLanguage = "fi";

            ((IDictionary<string, object>)p)[uiLanguage] = Translations.FetchTranslation(uiLanguage);
            if (userLang == "sv")
            {
                uiLanguage = userLang;
                ((IDictionary<string, object>)p)[uiLanguage] = Translations.FetchTranslation(uiLanguage);
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
            LoginProvider.LogUserAction(actionName, User?.Identity as ClaimsIdentity);
        }
    }

    public abstract class AnonymousBaseController : Controller
    {
        protected static IClock Clock => ClockFactory.SharedInstance;

        protected static ActionResult Json2(object data)
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

            public JsonSerializerSettings SerializerSettings { get; set; } = new();
            public Formatting Formatting { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                if (context == null)
                    throw new ArgumentNullException(nameof(context));

                var response = context.HttpContext.Response;

                response.ContentType = !string.IsNullOrEmpty(ContentType)
                    ? ContentType
                    : "application/json";

                if (ContentEncoding != null)
                    response.ContentEncoding = ContentEncoding;

                if (Data == null) return;
                var writer = new JsonTextWriter(response.Output) { Formatting = Formatting };

                var serializer = JsonSerializer.Create(SerializerSettings);
                serializer.Serialize(writer, Data);

                writer.Flush();
            }
        }
    }
}