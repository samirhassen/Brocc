using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NTech.Services.Infrastructure
{
    public class NTechHandleErrorAttribute : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext filterContext)
        {
            base.OnException(filterContext);
            var owinContext = filterContext?.HttpContext?.GetOwinContext();

            var isApi = (filterContext?.HttpContext?.Items?.Contains("ntech.isapi") ?? false) && (((bool)filterContext.HttpContext.Items["ntech.isapi"]));

            using (Serilog.Context.LogContext.PushProperties(NTechLoggingMiddleware.GetProperties(owinContext).ToArray()))
            {
                string controllerName = (string)filterContext?.RouteData?.Values["controller"];
                string actionName = (string)filterContext?.RouteData?.Values["action"];
                if (isApi)
                {
                    Log.Error(filterContext.Exception, "Error in api {action}.{controller}", actionName, controllerName);
                }
                else
                {
                    Log.Error(filterContext.Exception, "Exception in action {action}.{controller}", actionName, controllerName);
                }
            }

            if (isApi || (filterContext?.RequestContext?.HttpContext?.Request?.IsAjaxRequest() ?? false))
            {
                filterContext.Result = new HttpStatusCodeResult(500, "Internal server error");
            }
        }
    }
}
