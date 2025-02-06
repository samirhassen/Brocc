using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace nCreditReport.Controllers
{
    public abstract class NController : Controller
    {
        private static readonly Lazy<MemoryCache> cache = new Lazy<MemoryCache>(() => MemoryCache.Default);

        protected void GetUserProperties(List<string> errors, out int userId, out string username, out string informationMetadata)
        {
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

            username = GetClaim("ntech.username");
            if (string.IsNullOrWhiteSpace(username))
                errors.Add("Missing claim ntech.username");

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

        public ClaimsIdentity Identity
        {
            get
            {
                return this.User.Identity as ClaimsIdentity;
            }
        }

        protected string GetClaim(string name)
        {
            return (this?.User?.Identity as System.Security.Claims.ClaimsIdentity)?.FindFirst(name)?.Value;
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

        protected ActionResult Json2(object data, bool useCamelCase)
        {
            JsonSerializerSettings serializeSettings = new JsonSerializerSettings();
            if (useCamelCase)
            {
                serializeSettings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };
            }

            return new JsonNetResult
            {
                Data = data,
                SerializerSettings = serializeSettings
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