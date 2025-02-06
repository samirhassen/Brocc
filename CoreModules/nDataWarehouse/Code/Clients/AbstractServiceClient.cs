using NTech.Services.Infrastructure;
using System;

namespace nDataWarehouse.Code.Clients
{
    public abstract class AbstractServiceClient
    {
        private readonly Func<string> getBearerToken;

        public AbstractServiceClient(Func<string> getBearerToken)
        {
            this.getBearerToken = getBearerToken;
        }

        protected abstract string ServiceName { get; }

        protected NHttp.NHttpCall Begin(string bearerToken = null, TimeSpan? timeout = null)
        {
            return NHttp.Begin(new Uri(NEnv.ServiceRegistry.Internal[ServiceName]), bearerToken ?? getBearerToken(), timeout: timeout);
        }
    }
}