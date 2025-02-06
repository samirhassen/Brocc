using System;

namespace NTech.Core.Module
{
    public interface INTechServiceRegistry
    {
        Uri InternalServiceUrl(string serviceName, string relativeUrl, params Tuple<string, string>[] queryStringParameters);
        Uri ExternalServiceUrl(string serviceName, string relativeUrl, params Tuple<string, string>[] queryStringParameters);
        bool ContainsService(string serviceName);
    }
}
