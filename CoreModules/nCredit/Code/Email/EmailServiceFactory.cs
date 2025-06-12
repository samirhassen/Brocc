using System;
using System.Collections.Generic;
using CommonMark;
using NTech.Core.Module.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure.Email;
using Nustache.Core;

namespace nCredit.Code.Email
{
    public static class EmailServiceFactory
    {
        private static Lazy<INTechEmailServiceFactory> sharedInstance =
            new Lazy<INTechEmailServiceFactory>(() => new EmailServiceFactoryImpl());

        public static INTechEmailService CreateEmailService() => SharedInstance.CreateEmailService();

        public static bool HasEmailProvider => SharedInstance.HasEmailProvider;

        public static INTechEmailServiceFactory SharedInstance => sharedInstance.Value;

        public class EmailServiceFactoryImpl : INTechEmailServiceFactory
        {
            public INTechEmailService CreateEmailService()
            {
                var renderer = new EmailRenderer(
                    x => CommonMarkConverter.Convert(x),
                    ReplaceMustacheMines);
                var factory = new NTechEmailServiceFactory(renderer);
                return factory.CreateEmailService();
            }

            public bool HasEmailProvider => NTechEmailServiceFactory.HasEmailProvider;

            private string ReplaceMustacheMines(string template, Dictionary<string, object> mines)
            {
                // Support document_ClientData in mail templates
                var user = LegacyHttpServiceSystemUser.SharedInstance;
                var customerClient = LegacyServiceClientFactory.CreateCustomerClient(user, NEnv.ServiceRegistry);
                var service = new DocumentClientDataService(customerClient, NEnv.ClientCfgCore, NEnv.EnvSettings);

                mines = service.ExtendContextWithCommonContext(mines);

                return Render.StringToString(template, mines);
            }
        }
    }
}