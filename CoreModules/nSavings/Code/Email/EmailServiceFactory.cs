using NTech.Core.Module.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure.Email;
using System.Collections.Generic;

namespace nSavings.Code.Email
{
    public static class EmailServiceFactory
    {
        public static INTechEmailService CreateEmailService()
        {
            var renderer = new EmailRenderer(
                x => CommonMark.CommonMarkConverter.Convert(x),
                ReplaceMustacheMines);
            var factory = new NTechEmailServiceFactory(renderer);
            return factory.CreateEmailService();
        }

        public static bool HasEmailProvider => NTechEmailServiceFactory.HasEmailProvider;

        private static string ReplaceMustacheMines(string template, Dictionary<string, object> mines)
        {
            // Support document_ClientData in mail templates
            var customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
            var service = new DocumentClientDataService(customerClient, NEnv.ClientCfgCore, NEnv.EnvSettings);
            mines = service.ExtendContextWithCommonContext(mines);

            return Nustache.Core.Render.StringToString(template, mines);
        }
    }
}