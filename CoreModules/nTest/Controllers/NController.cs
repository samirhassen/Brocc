using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace nTest.Controllers
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
                return WithCache("nTest.Controllers.NController.GetUserDisplayNamesByUserId", () =>
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

        protected ActionResult Json2(object data, int? customHttpStatusCode = null, string customStatusDescription = null)
        {
            return new JsonNetResult
            {
                Data = data,
                CustomHttpStatusCode = customHttpStatusCode,
                CustomStatusDescription = customStatusDescription
            };
        }

        public class JsonNetResult : ActionResult
        {
            public Encoding ContentEncoding { get; set; }
            public string ContentType { get; set; }
            public object Data { get; set; }
            public int? CustomHttpStatusCode { get; set; }
            public string CustomStatusDescription { get; set; }

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

                if (CustomHttpStatusCode.HasValue)
                    response.StatusCode = CustomHttpStatusCode.Value;
                if (!string.IsNullOrWhiteSpace(CustomStatusDescription))
                    response.StatusDescription = CustomStatusDescription;

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