using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;

namespace NTech.Legacy.Module.Shared.Infrastructure.HttpClient
{
    public static class LegacyServiceClientFactory
    {
        public static ICustomerClient CreateCustomerClient(INHttpServiceUser user, NTechServiceRegistry serviceRegistry) =>
            new CustomerClient(user, CreateClientFactory(serviceRegistry));

        public static IUserClient CreateUserClient(INHttpServiceUser user, NTechServiceRegistry serviceRegistry) =>
            new UserClient(user, CreateClientFactory(serviceRegistry));

        public static IDocumentClient CreateDocumentClient(INHttpServiceUser user, NTechServiceRegistry serviceRegistry) =>
            new DocumentClient(user, CreateClientFactory(serviceRegistry));

        public static ICreditClient CreateCreditClient(INHttpServiceUser user, NTechServiceRegistry serviceRegistry) =>
            new CreditClient(user, CreateClientFactory(serviceRegistry));

        public static ISavingsClient CreateSavingsClient(INHttpServiceUser user, NTechServiceRegistry serviceRegistry) =>
            new SavingsClient(user, CreateClientFactory(serviceRegistry));

        public static IPreCreditClient CreatePreCreditClient(INHttpServiceUser user, NTechServiceRegistry serviceRegistry) =>
            new PreCreditClient(user, CreateClientFactory(serviceRegistry));

        public static IAuditClient CreateAuditClient(INHttpServiceUser user, NTechServiceRegistry serviceRegistry) =>
            new AuditClient(user, CreateClientFactory(serviceRegistry));

        public static ServiceClientFactory CreateClientFactory(NTechServiceRegistry serviceRegistry) => new ServiceClientFactory(
                serviceRegistry.Internal.ServiceRootUri,
                () => new System.Net.Http.HttpClient(),
                new ServiceClientSyncConverterLegacy());
    }
}
