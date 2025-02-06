using Newtonsoft.Json;
using NTech.Core.Module.Shared.Clients;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Concurrent;

namespace nCustomerPages.Code.Clients
{
    public class InternalServiceProxyClient
    {
        private static readonly ConcurrentDictionary<string, ServiceClient> clients = new ConcurrentDictionary<string, ServiceClient>(StringComparer.OrdinalIgnoreCase);

        private ServiceClient GetServiceClient(string serviceName)
        {
            Func<ServiceClient> createClient = () =>
            {
                var clientFactory = LegacyServiceClientFactory.CreateClientFactory(NEnv.ServiceRegistry);
                return clientFactory.CreateClient(LegacyHttpServiceSystemUser.SharedInstance, serviceName);
            };
            return clients.AddOrUpdate(serviceName, (_) => createClient(), (_, existingClient) => existingClient);
        }

        public RawJsonActionResult Post<TRequest>(string serviceName, string relativeUrl, TRequest request)
        {
            var client = GetServiceClient(serviceName);
            return client.ToSync(() => client.Call(
                    x => x.PostJson(relativeUrl, request),
                    async x =>
                    {
                        if (x.IsSuccessStatusCode)
                        {
                            var jsonData = await x.ParseAsRawJson();
                            return new RawJsonActionResult
                            {
                                JsonData = await x.ParseAsRawJson()
                            };
                        }
                        else if (x.IsApiError)
                        {
                            var apiError = await x.ParseApiError();
                            return new RawJsonActionResult
                            {
                                IsNTechApiError = true,
                                JsonData = JsonConvert.SerializeObject(apiError),
                                CustomHttpStatusCode = x.StatusCode,
                                CustomStatusDescription = x.ReasonPhrase
                            };
                        }
                        else
                        {
                            return new RawJsonActionResult
                            {
                                IsNTechApiError = true,
                                JsonData = JsonConvert.SerializeObject(new ServiceClient.NHttpCallResult.ApiError
                                {
                                    ErrorCode = "unknown",
                                    ErrorMessage = "Unknown error"
                                }),
                                CustomHttpStatusCode = 500,
                                CustomStatusDescription = "Internal server error"
                            };
                        }
                    }));
        }
    }
}