using System;
using NTech.Services.Infrastructure;

namespace nSavings.Code
{
    public abstract class AbstractServiceClient
    {
        protected abstract string ServiceName { get; }

        protected NHttp.NHttpCall Begin(string bearerToken = null, TimeSpan? timeout = null)
        {
            return NHttp.Begin(new Uri(NEnv.ServiceRegistry.Internal[ServiceName]),
                bearerToken ?? NHttp.GetCurrentAccessToken(), timeout: timeout);
        }
    }
}