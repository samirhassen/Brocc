using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.Caching;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace nDataWarehouse.Controllers
{
    public abstract class NController : Controller
    {
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

        /// <summary>
        /// Set in controller:
        ///  ViewBag.JsonInitialData = EncodeInitialData(new { [...] })
        ///
        /// Parse in cshtml:
        /// <script>
        ///   initialData = JSON.parse(atob('@Html.Raw(ViewBag.JsonInitialData)'))
        /// </script>
        ///
        /// </summary>
        protected string EncodeInitialData<T>(T data)
        {
            return Convert.ToBase64String(Encoding.GetEncoding("iso-8859-1").GetBytes(JsonConvert.SerializeObject(data)));
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
            var d = UserDisplayNamesByUserId;
            if (d.ContainsKey(userId))
                return d[userId];
            else
                return $"User {userId}";
        }

        protected Dictionary<string, string> UserDisplayNamesByUserId
        {
            get
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

        protected dynamic GetTranslations()
        {
            var userLang = System.Threading.Thread.CurrentThread?.CurrentUICulture?.TwoLetterISOLanguageName;
            return NTechCache.WithCache($"ntech.nprecredit.GetTranslations.{userLang ?? "en"}", TimeSpan.FromHours(1), () => GetTranslationsI(userLang));
        }

        private dynamic GetTranslationsI(string userLang)
        {
            var p = new ExpandoObject();

            var uiLanguage = "en";

            (p as IDictionary<string, object>)["en"] = new Dictionary<string, string>(); //TODO: Fetch real translations

            if (userLang == "fi" || userLang == "sv")
            {
                uiLanguage = userLang;
                (p as IDictionary<string, object>)[userLang] = new Dictionary<string, string>(); //TODO: Fetch real translations
            }

            return new
            {
                uiLanguage = uiLanguage,
                translateUrl = Url.Action("Translation", "Common"),
                translations = p
            };
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