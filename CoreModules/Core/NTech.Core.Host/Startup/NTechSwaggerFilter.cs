using Microsoft.OpenApi.Models;
using NTech.Core.Module.Shared.Infrastructure;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NTech.Core.Host.Startup
{
    /// <summary>
    /// Ensures that controllers decorated with something like the below example are not included in the swagger if the feature are not enabled.
    /// This works in combination with FeatureToggleActionFilter that also makes sure that the apis cannot be called in this case.
    /// [NTechRequireFeatures(RequireFeaturesAll = new string[] { "ntech.feature.perloandueday" })]
    /// </summary>
    public class NTechSwaggerFilter : IDocumentFilter
    {
        private readonly IClientConfigurationCore clientConfiguration;

        public NTechSwaggerFilter(IClientConfigurationCore clientConfiguration)
        {
            this.clientConfiguration = clientConfiguration;
        }
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            foreach (var apiDescription in context.ApiDescriptions)
            {
                var featureAttribute = apiDescription.CustomAttributes().OfType<NTechRequireFeaturesAttribute>().FirstOrDefault() as NTechRequireFeaturesAttribute;
                if (featureAttribute == null)
                {
                    continue;
                }
                if (!featureAttribute.IsEnabled(clientConfiguration.IsFeatureEnabled, clientConfiguration.Country.BaseCountry))
                {
                    var key = "/" + apiDescription.RelativePath.TrimEnd('/');
                    swaggerDoc.Paths.Remove(key);
                }
            }
        }
    }
}
