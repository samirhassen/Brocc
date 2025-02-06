using System.Web.Mvc;

namespace NTech.Services.Infrastructure
{
    /// <summary>
    /// Markes a controller or action as an api rather than a view. 
    /// 
    /// Used for things like overriding the insane default error handling that returns a 200 OK and html ... not so great for services.
    /// </summary>
    public class NTechApiAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var i = filterContext?.HttpContext?.Items;
            if (i != null)
                i["ntech.isapi"] = true;
            base.OnActionExecuting(filterContext);
        }
    }
}
