using System;
using System.Collections.Generic;
using CommonMark;
using nCustomer.DbModel;
using NTech.Core.Customer.Shared.Services.Settings;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure.Email;
using Nustache.Core;

namespace nCustomer.Code.Email
{
    public static class EmailServiceFactory
    {
        private static Lazy<ICustomerClientLoadSettingsOnly> settingsClient = new Lazy<ICustomerClientLoadSettingsOnly>(
            () => new DirectDbLoadSettingsOnlyCustomerClient(
                () => new CustomersContext(), CoreClock.SharedInstance, NEnv.ClientCfgCore));

        public static INTechEmailService CreateEmailService()
        {
            var renderer = new EmailRenderer(
                x => CommonMarkConverter.Convert(x),
                ReplaceMustacheMines);
            var factory = new NTechEmailServiceFactory(renderer);
            return factory.CreateEmailService();
        }

        private static string ReplaceMustacheMines(string template, Dictionary<string, object> mines)
        {
            // Support document_ClientData in mail templates
            var service = new DocumentClientDataService(settingsClient.Value, NEnv.ClientCfgCore, NEnv.EnvSettings);
            mines = service.ExtendContextWithCommonContext(mines);

            return Render.StringToString(template, mines);
        }

        public class ServiceFactoryImpl : INTechEmailServiceFactory
        {
            public bool HasEmailProvider => NTechEmailServiceFactory.HasEmailProvider;

            public INTechEmailService CreateEmailService() => EmailServiceFactory.CreateEmailService();
        }
    }
}