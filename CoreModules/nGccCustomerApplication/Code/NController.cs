using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace nGccCustomerApplication
{
    public abstract class NController : Controller
    {
        protected ActionResult Json2(object data)
        {
            return new JsonNetResult
            {
                Data = data
            };
        }

        protected string GetClaim(string name)
        {
            return (this
                    .HttpContext
                    ?.User
                    ?.Identity as System.Security.Claims.ClaimsIdentity)
                ?.FindFirst(name)
                ?.Value;
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

        protected bool TryParseJsonRequestAs<TRequest>(out TRequest result, out string failedMessage) where TRequest : class
        {
            Request.InputStream.Position = 0;
            using (var r = new StreamReader(Request.InputStream))
            {
                var requestString = r.ReadToEnd();
                try
                {
                    result = JsonConvert.DeserializeObject<TRequest>(requestString);
                    failedMessage = null;
                    return true;
                }
                catch (JsonReaderException ex)
                {
                    failedMessage = ex.Message;
                    result = null;
                    return false;
                }
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