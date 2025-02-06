using System;
using System.Web;
using System.Web.Mvc;

namespace nCustomerPages.Code
{
    /// <summary>
    /// Some customers complained that after logging out the could scroll back thorugh the history and see the cached pages.
    /// To prevent having to deal with support issues around this and trying to explain to non technical people
    /// how this differs from "still being logged in" we turn off all the caching for these pages which seems to cause at least
    /// most browsers from keeping this page in the browser history.
    /// 
    /// For our angular apps this is not needed.
    /// 
    /// See https://stackoverflow.com/questions/14437987/how-disable-browser-back-button-only-after-logout-in-mvc3-net
    /// </summary>
    public class PreventBackButtonAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if(filterContext.Result is ViewResult && !NEnv.IsBackButtonPreventDisabled)
            {
                filterContext.HttpContext.Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
                filterContext.HttpContext.Response.Cache.SetValidUntilExpires(false);
                filterContext.HttpContext.Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
                filterContext.HttpContext.Response.Cache.SetCacheability(HttpCacheability.NoCache);
                filterContext.HttpContext.Response.Cache.SetNoStore();
            }
            base.OnResultExecuting(filterContext);
        }
    }
}