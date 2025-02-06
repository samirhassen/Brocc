using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;

namespace NTech.Core.Host.Infrastructure
{
    /// <summary>
    /// Used for pages/services created before the migration to keep working with only adding a header instead of rewriting to handle the correct case everywhere.
    /// Dont use this if it's easy to change the calling page/service.
    /// </summary>
    public class PreserveCaseActionFilter : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null) return;

            var preserveCaseHeaders = context.HttpContext.Request.Headers["X-NTech-Preserve-Case"];
            if (!preserveCaseHeaders.Any(value => value == "1")) return;

            if (context.Result is ObjectResult objectResult)
            {
                var mediaTypeHeaderValue = MediaTypeHeaderValue.Parse("application/json");
                mediaTypeHeaderValue.Encoding = Encoding.UTF8;
                context.Result = new ContentResult
                {
                    Content = JsonConvert.SerializeObject(objectResult.Value),
                    ContentType = mediaTypeHeaderValue.ToString()
                };
            }
        }

        public void OnActionExecuting(ActionExecutingContext context) { }
    }
}
