using Serilog;
using System;
using System.Web.Mvc;

namespace nCustomerPages.Code
{
    public abstract class AuthenticationAttributeBase : ActionFilterAttribute
    {
        protected abstract void DoOnActionExecuting(ActionExecutingContext filterContext);
        protected abstract string AttributeErrorLoggingName { get; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                DoOnActionExecuting(filterContext);
            }
            catch (Exception ex)
            {
                NLog.Error(ex, $"Error in {AttributeErrorLoggingName}");
                filterContext.HttpContext.Response.Clear();
                filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
                filterContext.HttpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                filterContext.Result = new HttpStatusCodeResult(System.Net.HttpStatusCode.InternalServerError, "Server error during authentication");
            }
        }
    }
}