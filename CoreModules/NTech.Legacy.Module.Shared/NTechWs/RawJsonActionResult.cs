using System;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace NTech.Services.Infrastructure.NTechWs
{
    public class RawJsonActionResult : ActionResult
    {
        public Encoding ContentEncoding { get; set; }
        public string ContentType { get; set; }
        public string JsonData { get; set; }
        public int? CustomHttpStatusCode { get; set; }
        public string CustomStatusDescription { get; set; }

        public bool IsNTechApiError { get; set; }

        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            HttpResponseBase response = context.HttpContext.Response;

            if (IsNTechApiError)
            {
                response.TrySkipIisCustomErrors = true;
                response.Headers["X-Ntech-Api-Error"] = "1";
            }

            response.ContentType = !string.IsNullOrEmpty(ContentType)
                ? ContentType
                : "application/json";

            if (ContentEncoding != null)
                response.ContentEncoding = ContentEncoding;

            if (CustomHttpStatusCode.HasValue)
                response.StatusCode = CustomHttpStatusCode.Value;
            if (!string.IsNullOrWhiteSpace(CustomStatusDescription))
                response.StatusDescription = CustomStatusDescription;

            if (JsonData != null)
            {
                response.Output.Write(JsonData);
            }
        }
    }
}