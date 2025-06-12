using System;
using System.Web.Mvc;
using Newtonsoft.Json.Serialization;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;

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
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ConvertJsonToCamelCaseActionFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var preserveCaseHeaderValue = filterContext.HttpContext.Request.Headers["X-NTech-Force-CamelCase"];
            switch (preserveCaseHeaderValue)
            {
                case "1" when filterContext.Result is JsonNetActionResult jsonResult:
                    jsonResult.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    break;
                case "1"
                    when filterContext.Result is RawJsonActionResult:
                    break;
            }

            base.OnActionExecuted(filterContext);
        }
    }
}