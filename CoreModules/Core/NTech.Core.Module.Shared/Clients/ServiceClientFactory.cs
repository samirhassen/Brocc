using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Net.Http;

namespace NTech.Core.Module.Shared.Clients
{
    public class ServiceClientFactory
    {
        private readonly Func<string, Uri> getServiceRootUri;
        private readonly Func<HttpClient> createHttpClient;
        private readonly IServiceClientSyncConverter serviceClientSyncConverter;

        public ServiceClientFactory(Func<string, Uri> getServiceRootUri, Func<HttpClient> createHttpClient, IServiceClientSyncConverter serviceClientSyncConverter)
        {
            this.getServiceRootUri = getServiceRootUri;
            this.createHttpClient = createHttpClient;
            this.serviceClientSyncConverter = serviceClientSyncConverter;
        }

        public ServiceClient CreateClient(INHttpServiceUser user, string serviceName)
        {
            return new ServiceClient(user, getServiceRootUri, createHttpClient, serviceClientSyncConverter, serviceName);
        }
    }
}
