using nPreCredit.Code.Services.SharedStandard;
using NTech.Core.Module.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure.Email;
using System.Collections.Generic;

namespace nPreCredit.Code.Email
{
    public static class EmailServiceFactory
    {
        public static INTechEmailService CreateEmailService()
        {
            var markdownService = new MarkdownTemplateRenderingServiceLegacyPreCredit();
            var renderer = new EmailRenderer(
                markdownService.RenderTemplateToHtml,
                ReplaceMustacheMines);
            var factory = new NTechEmailServiceFactory(renderer);
            return factory.CreateEmailService();
        }

        private static string ReplaceMustacheMines(string template, Dictionary<string, object> mines)
        {
            // Support document_ClientData in mail templates
            var customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
            var service = new DocumentClientDataService(customerClient, NEnv.ClientCfgCore, NEnv.EnvSettings);
            mines = service.ExtendContextWithCommonContext(mines);

            return Nustache.Core.Render.StringToString(template, mines);
        }

        public class ServiceFactoryImpl : INTechEmailServiceFactory
        {
            public bool HasEmailProvider => NTechEmailServiceFactory.HasEmailProvider;

            public INTechEmailService CreateEmailService() => EmailServiceFactory.CreateEmailService();
        }
    }
}