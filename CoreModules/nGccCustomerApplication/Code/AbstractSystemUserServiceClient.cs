using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace nGccCustomerApplication.Code
{
    public abstract class AbstractSystemUserServiceClient
    {
        protected abstract string ServiceName { get; }

        protected NTech.Services.Infrastructure.NHttp.NHttpCall Begin(string bearerToken = null, TimeSpan? timeout = null)
        {
            return NTech.Services.Infrastructure.NHttp.Begin(new Uri(NEnv.ServiceRegistry.Internal[ServiceName]), bearerToken ?? NEnv.SystemUserBearerToken, timeout: timeout);
        }

        protected Uri CreateServiceUri(string serviceName, string relativePath, params Tuple<string, string>[] args)
        {
            var serviceUrl = new Uri(new Uri(NEnv.ServiceRegistry.Internal[serviceName]), relativePath);
            if (relativePath.Contains("?"))
                throw new Exception("Put the arguments in args not in relativePath");
            if (args.Length > 0)
            {
                return new Uri(serviceUrl.AbsoluteUri + "?" + string.Join("&", args.Select(x => $"{x.Item1}={x.Item2}")));
            }
            else
            {
                return serviceUrl;
            }
        }
    }
}