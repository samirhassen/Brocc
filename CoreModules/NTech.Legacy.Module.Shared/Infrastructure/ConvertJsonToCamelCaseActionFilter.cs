using Newtonsoft.Json.Serialization;
using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace NTech.Legacy.Module.Shared.Infrastructure
{
    /// <summary>
    /// Core apis are camelCased by default and we want to allow an api to be based on a shared service and exist both in legacy and core
    /// with a feature toggle that switches which api is called.
    /// When called from a legacy app this means opting out of camel case when calling an api that is on the legacy side. This is handled by a PreserveCaseActionFilter in the core host.
    /// When called from a core app this means opting in to camel case when calling an api that is on the legacy side. This last case is what this filter handles.
    /// 
    /// Add this to a service using in Global_asax Application_Start like this:
    /// GlobalFilters.Filters.Add(new ConvertJsonToCamelCaseActionFilterAttribute());
    /// </summary>
    public class ConvertJsonToCamelCaseActionFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var preseveCaseHeaderValue = filterContext.HttpContext.Request.Headers["X-NTech-Force-CamelCase"];
            if (preseveCaseHeaderValue == "1" && filterContext.Result is JsonNetActionResult jsonResult)
            {
                jsonResult.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            }
            else if (preseveCaseHeaderValue == "1" && filterContext.Result is NTech.Services.Infrastructure.NTechWs.RawJsonActionResult jsonResult2)
            {

            }
            base.OnActionExecuted(filterContext);
        }
    }
}
