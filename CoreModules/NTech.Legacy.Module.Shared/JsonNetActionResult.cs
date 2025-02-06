using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace NTech.Services.Infrastructure
{
    public class JsonNetActionResult : ActionResult
    {
        public Encoding ContentEncoding { get; set; }
        public string ContentType { get; set; }
        public object Data { get; set; }
        public int? CustomHttpStatusCode { get; set; }
        public string CustomStatusDescription { get; set; }
        public Dictionary<string, string> CustomHeaders { get; set; }

        public JsonSerializerSettings SerializerSettings { get; set; }
        public Formatting Formatting { get; set; }

        public JsonNetActionResult()
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

            if (CustomHeaders != null)
            {
                foreach (var header in CustomHeaders)
                {
                    response.Headers.Add(header.Key, header.Value);
                }
            }

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