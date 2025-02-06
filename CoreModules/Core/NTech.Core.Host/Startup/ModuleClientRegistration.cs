using NTech.Core.Module;
using NTech.Core.Module.Infrastrucutre;
using NTech.Core.Module.Infrastrucutre.HttpClient;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;

namespace NTech.Core.Host.Startup
{
    internal static class ModuleClientRegistration
    {
        public static void AddModuleClients(IServiceCollection services, NEnv env)
        {
            services.AddScoped<INHttpServiceUser, NHttpServiceHttpContextUser>();
            services.AddSingleton<NHttpServiceSystemUser>();
            services.AddSingleton<IServiceClientSyncConverter, ServiceClientSyncConverterCore>();
            services.AddSingleton<ServiceClientFactory>(x =>
            {
                var httpClientFactory = x.GetRequiredService<IHttpClientFactory>();
                var syncConverter = x.GetRequiredService<IServiceClientSyncConverter>();
                return new ServiceClientFactory(env.ServiceRegistry.Internal.ServiceRootUri, httpClientFactory.CreateClient, syncConverter);
            });

            services.AddScoped<IAuditClient, AuditClient>();
            services.AddHttpClient<AuditClient>().SetHandlerLifetime(TimeSpan.FromMinutes(5));

            services.AddScoped<ICustomerClient, CustomerClient>();
            services.AddScoped<ICustomerClientLoadSettingsOnly, CustomerClient>();
            services.AddHttpClient<CustomerClient>().SetHandlerLifetime(TimeSpan.FromMinutes(5));

            services.AddScoped<IUserClient, UserClient>();
            services.AddHttpClient<UserClient>().SetHandlerLifetime(TimeSpan.FromMinutes(5));

            services.AddScoped<IDocumentClient, DocumentClient>();
            services.AddHttpClient<DocumentClient>().SetHandlerLifetime(TimeSpan.FromMinutes(5));

            services.AddScoped<IPreCreditClient, PreCreditClient>();
            services.AddHttpClient<PreCreditClient>().SetHandlerLifetime(TimeSpan.FromMinutes(5));

            if (!env.IsProduction)
            {
                services.AddScoped<ITestClient, TestClient>();
                services.AddHttpClient<TestClient>().SetHandlerLifetime(TimeSpan.FromMinutes(5));
            }

            var encryptionKeySet = new Lazy<EncryptionKeySet>(() =>
            {
                var data = File.ReadAllText(env.StaticResourceFile("ntech.encryption.keysfile", "encryptionkeys.txt", true).FullName);
                return EncryptionKeySet.ParseFromString(data);
            });
            services.AddScoped(x =>
            {
                var clock = x.GetRequiredService<ICoreClock>();
                var user = x.GetRequiredService<INTechCurrentUserMetadata>();
                return new EncryptionService(encryptionKeySet.Value.CurrentKeyName, encryptionKeySet.Value.AsDictionary(), clock, user);
            });
        }
    }
}
