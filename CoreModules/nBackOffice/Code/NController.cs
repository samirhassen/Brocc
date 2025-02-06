using nBackOffice.Code;
using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace nBackOffice
{
    public abstract class NController : Controller
    {
        private static readonly Lazy<MemoryCache> cache = new Lazy<MemoryCache>(() => MemoryCache.Default);

        protected T WithCache<T>(string key, Func<T> produce, TimeSpan duration) where T : class
        {
            var val = cache.Value.Get(key) as T;
            if (val != null)
                return val;
            val = produce();
            cache.Value.Set(key, val, DateTimeOffset.Now.Add(duration));
            return val;
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

        protected static Uri AppendQueryStringParam(Uri uri, string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return uri;

            var uriBuilder = new UriBuilder(uri);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query[name] = value;
            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri;
        }

        public ClaimsIdentity Identity
        {
            get
            {
                return this.User.Identity as ClaimsIdentity;
            }
        }

        public int LoggedInUserId
        {
            get
            {
                return int.Parse(this.Identity?.FindFirst("ntech.userid").Value);
            }
        }

        protected new ActionResult Json(object data)
        {
            return new JsonNetResult
            {
                Data = data
            };
        }

        protected List<TopLevelFunctionListModel.MenuGroup> Menu
        {
            get
            {
                return MenuWithSubGroups.Item1;
            }
        }

        protected Tuple<List<TopLevelFunctionListModel.MenuGroup>, List<string>> MenuWithSubGroups
        {
            get
            {
                return NTechCache.WithCache($"b33e1308-acca-4c8a-af16-7a93afd6s93c", TimeSpan.FromDays(1), () =>
                {
                    var model = TopLevelFunctionListModel.FromEmbeddedResource();
                    return model.GetMenuWithSubGroupOrder();
                });
            }
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