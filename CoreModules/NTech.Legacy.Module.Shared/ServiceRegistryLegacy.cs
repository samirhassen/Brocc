using NTech.Core.Module;
using NTech.Services.Infrastructure;
using System;

namespace NTech.Legacy.Module.Shared
{
    public class ServiceRegistryLegacy : INTechServiceRegistry
    {
        private readonly NTechServiceRegistry serviceRegistry;

        public ServiceRegistryLegacy(NTechServiceRegistry serviceRegistry)
        {
            this.serviceRegistry = serviceRegistry;
        }

        public Uri ExternalServiceUrl(string serviceName, string relativeUrl, params Tuple<string, string>[] queryStringParameters) =>
            serviceRegistry.External.ServiceUrl(serviceName, relativeUrl, queryStringParameters);

        public Uri InternalServiceUrl(string serviceName, string relativeUrl, params Tuple<string, string>[] queryStringParameters) =>
            serviceRegistry.Internal.ServiceUrl(serviceName, relativeUrl, queryStringParameters);

        public bool ContainsService(string serviceName) => serviceRegistry.ContainsService(serviceName);
    }
}
