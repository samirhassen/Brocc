using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using NTech.Core.Module.Shared.Infrastructure;
using System.Reflection;

namespace NTech.Core.Host.Infrastructure
{
    /// <summary>
    /// Ensures that controllers decorated with something like the below example return 404 on all actions if the feature are not enabled.
    /// This works in combination with NTechSwaggerFilter that also makes sure that the apis dont show up in the documentation.
    /// [NTechRequireFeatures(RequireFeaturesAll = new string[] { "ntech.feature.perloandueday" })]
    /// </summary>
    public class FeatureToggleActionFilter : IActionFilter
    {
        private readonly IClientConfigurationCore clientConfigurationCore;

        public FeatureToggleActionFilter(IClientConfigurationCore clientConfigurationCore)
        {
            this.clientConfigurationCore = clientConfigurationCore;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {

        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            if (controllerActionDescriptor == null) return;

            var requiredFeaturesAttribute = controllerActionDescriptor.ControllerTypeInfo.GetCustomAttribute<NTechRequireFeaturesAttribute>();
            if (requiredFeaturesAttribute == null) return;

            if (!requiredFeaturesAttribute.IsEnabled(clientConfigurationCore.IsFeatureEnabled, clientConfigurationCore.Country.BaseCountry))
            {
                context.Result = new NotFoundResult();
            }
        }
    }
}
