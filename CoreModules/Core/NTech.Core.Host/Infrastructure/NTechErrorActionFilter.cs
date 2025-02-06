using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NTech.Core.Host.Logging;
using NTech.Core.Module.Shared.Infrastructure;

namespace NTech.Core.Host.Infrastructure
{
    /// <summary>
    /// Traps all errors, logs them and returns a consitent { errorCode, errorMessage} + http status code to the user.
    /// Throw NTechCoreWebserviceException from services to take control over how these are presented to the user.
    /// Normal exceptions are presented as code = generic, message = generic and http status = 500
    /// </summary>
    public class NTechErrorActionFilter : IActionFilter
    {
        private readonly ILogger<NTechErrorActionFilter> logger;

        public NTechErrorActionFilter(ILogger<NTechErrorActionFilter> logger)
        {
            this.logger = logger;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if(context.Exception == null) return;

            var httpContext = context?.HttpContext;
            var request = httpContext?.Request;

            var user = new NTechCurrentUserMetadataImpl(httpContext?.User?.Identity);

            var loggedException = NTechLogger.WrapException(context.Exception,
                remoteIp: httpContext?.Connection?.RemoteIpAddress?.ToString(),
                requestUri: request == null ? null : request.Path.ToString().TrimStart('/'),
                userId: user.OptionalUserId?.ToString());

            var wsException = context.Exception as NTechCoreWebserviceException;
            if (wsException != null && !wsException.IsUserFacing)
                wsException = null;

            if(wsException?.IsUserFacing != true)
                logger.LogError(loggedException, "Exception in api controller");

            var result = new JsonResult(new
            {
                errorMessage = wsException?.Message ?? "generic",
                errorCode = wsException?.ErrorCode ?? "generic"
            });
            result.StatusCode = wsException?.ErrorHttpStatusCode ?? 500;
            if(context.HttpContext != null)
            {
                context.HttpContext.Response.Headers.Add("X-Ntech-Api-Error", "1");
            }
            context.Result = result;
            context.ExceptionHandled = true;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {

        }
    }
}
