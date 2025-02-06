using Newtonsoft.Json;
using System;
using System.Runtime.Caching;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace nAudit.Controllers
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

        protected T WithCache<T>(string key, Func<T> produce) where T : class
        {
            var val = cache.Value.Get(key) as T;
            if (val != null)
                return val;
            val = produce();
            cache.Value.Set(key, val, DateTimeOffset.Now.AddMinutes(5));
            return val;
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