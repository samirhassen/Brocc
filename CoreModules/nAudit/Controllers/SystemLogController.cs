using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace nAudit.Controllers
{
    public class SystemLogItemModel
    {
        public DateTimeOffset? EventDate { get; set; }
        public string Level { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
    }

    [NTechAuthorizeAdmin]
    public class SystemLogController : Controller
    {
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> CreateBatch(List<SystemLogItemModel> items)
        {
            if (!NEnv.IsLegacyEndpointsEnabled)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.InternalServerError, "Method removed");

            if (!Global.IsInitialized)
                return new EmptyResult();

            using (var context = new AuditContext())
            {
                if (items != null)
                {
                    var logItems = items.Select(ToSystemLogItem);

                    context.SystemLogItems.AddRange(logItems);
                    await context.SaveChangesAsync();
                }
            }
            return new EmptyResult();
        }

        private static string StripRequestUri(string uri)
        {
            try
            {
                if (uri == null)
                {
                    return null;
                }
                else if (!uri.StartsWith("/"))
                {
                    uri = "/" + uri;
                }
                var u = new Uri(new Uri("http://localhost"), uri);
                return u.GetComponents(UriComponents.Path, UriFormat.Unescaped);
            }
            catch
            {
                return null;
            }
        }

        public static SystemLogItem ToSystemLogItem(SystemLogItemModel x)
        {
            Func<string, string> emptyToNull = s => string.IsNullOrWhiteSpace(s) ? null : s;
            if (x.Properties == null)
                x.Properties = new Dictionary<string, string>(1);

            List<string> usedProperties = new List<string>();
            Func<IDictionary<string, string>, string, string> prop = (d, n) =>
            {
                usedProperties.Add(n);
                if (!d.ContainsKey(n))
                    return null;
                var v = d[n];
                if (string.IsNullOrWhiteSpace(v))
                    return null;
                if (v.StartsWith("\"") && v.EndsWith("\""))
                    v = v.Substring(1, v.Length - 2);
                return v;
            };

            Func<string, int, string> clipLeft = (s, n) =>
            {
                if (s == null)
                    return null;
                if (s.Length > n)
                    return s.Substring(s.Length - n);
                else
                    return s;
            };
            var now = DateTimeOffset.Now;
            return new SystemLogItem
            {
                EventDate = x.EventDate ?? now,
                Level = x.Level,
                Message = emptyToNull(x.Message),
                RemoteIp = prop(x.Properties, "RemoteIp"),
                RequestUri = clipLeft(StripRequestUri(prop(x.Properties, "RequestUri")), 128),
                ServiceName = prop(x.Properties, "ServiceName"),
                ServiceVersion = prop(x.Properties, "ServiceVersion"),
                UserId = prop(x.Properties, "UserId"),
                EventType = clipLeft(prop(x.Properties, "EventType"), 128),
                ExceptionMessage = emptyToNull(x.Exception),
                ExceptionData = GetExceptionData(x, usedProperties)
            };
        }

        private static string GetExceptionData(SystemLogItemModel x, List<string> usedProperties)
        {
            if (x.Level != "Error" || x.Properties == null)
                return null;
            var p = FilterProperties(x.Properties, usedProperties, "action", "controller", "MachineName");
            if (p.Count == 0)
                return null;
            return string.Join("; ", p.Select(y => $"{y.Key}={y.Value}"));
        }

        private static IDictionary<string, string> FilterProperties(IDictionary<string, string> p, IList<string> names, params string[] additionalNames)
        {
            if (p == null)
                return p;
            else
                return p.Where(x => !names.Contains(x.Key) && !additionalNames.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
        }

        [HttpPost]
        public ActionResult FetchLatestErrors(int page = 0)
        {
            if (!NEnv.IsLegacyEndpointsEnabled)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.InternalServerError, "Method removed");

            using (var context = new AuditContext())
            {
                var result = context
                    .SystemLogItems
                    .Where(x => x.Level == "Error")
                    .OrderByDescending(x => x.Id)
                    .Skip(page * 20)
                    .Take(20)
                    .Select(x => new
                    {
                        x.ServiceName,
                        x.EventDate,
                        x.Message,
                        x.ExceptionMessage,
                        x.RemoteIp,
                        x.RequestUri
                    })
                    .ToList();
                return Json2(result);
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

                System.Web.HttpResponseBase response = context.HttpContext.Response;

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