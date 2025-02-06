using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nCustomer.Code.Services;
using nCustomer.Code.Services.Kyc;
using NTech.Core.Customer.Database;
using NTech.Core.Customer.Shared;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Customer.Shared.Services;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;

namespace NTech.Core.Customer
{
    public class CustomerNTechModule : NTechModule
    {
        public override string ServiceName => "nCustomer";

        public override void AddServices(IServiceCollection services, NEnv env)
        {
            services.AddScoped<ICustomerEnvSettings, CustomerEnvSettings>();
            services.AddScoped(x =>
            {
                var user = x.GetRequiredService<INTechCurrentUserMetadata>();
                var clock = x.GetRequiredService<ICoreClock>();
                return new CustomerContextFactory(() => new CustomerContextExtended(user, clock));
            });
            services.AddScoped<CustomerCheckPointService>();
            services.AddScoped<KycQuestionsTemplateService>();
            services.AddScoped<KycAnswersUpdateService>();
            services.AddScoped<KycQuestionsSessionService>();
            services.AddScoped<CustomerPropertyStatusService>();
            services.AddScoped<UrlService>();
            services.AddScoped(x =>
            {
                var user = x.GetRequiredService<INTechCurrentUserMetadata>();
                var clock = x.GetRequiredService<ICoreClock>();
                var clientConfig = x.GetRequiredService<IClientConfigurationCore>();
                var encryptionService = x.GetRequiredService<EncryptionService>();
                return new KycManagementService(
                    x.GetRequiredService<CustomerContextFactory>(),
                    x => new nCustomer.CustomerWriteRepository(x, user, clock, encryptionService, clientConfig),
                    x.GetRequiredService<UrlService>(), clientConfig, x.GetRequiredService<KycAnswersUpdateService>());
            });
            services.AddScoped(x =>
            {
                var clock = x.GetRequiredService<ICoreClock>();
                var encryptionService = x.GetRequiredService<EncryptionService>();
                var clientConfig = x.GetRequiredService<IClientConfigurationCore>();
                return new CustomerKycDefaultsService(
                    x.GetRequiredService<KycManagementService>(),
                    x.GetRequiredService<CustomerPropertyStatusService>(),
                    x.GetRequiredService<IClientConfigurationCore>(),
                    x.GetRequiredService<CustomerContextFactory>(),
                    (x, y) => new nCustomer.CustomerWriteRepository(x, y, clock, encryptionService, clientConfig));
            });
            services.AddScoped(x => new CrossModuleClientFactory(
                new Lazy<ICreditClient>(() => x.GetRequiredService<ICreditClient>()),
                new Lazy<ISavingsClient>(() => x.GetRequiredService<ISavingsClient>()),
                new Lazy<IPreCreditClient>(() => x.GetRequiredService<IPreCreditClient>())));
        }

        public override void OnApplicationStarted(ILogger logger)
        {

        }
    }
}