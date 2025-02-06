using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;

namespace nBackOffice.Code
{
    public class CustomerClient
    {
        protected NHttp.NHttpCall Begin(string bearerToken = null, TimeSpan? timeout = null)
        {
            return NHttp.Begin(new Uri(NEnv.ServiceRegistry.Internal["nCustomer"]), bearerToken ?? NHttp.GetCurrentAccessToken(), timeout: timeout);
        }

        public LoadSettingValuesResponse LoadSettings(string key)
        {
            return Begin()
                .PostJson("api/Settings/LoadValues", new { SettingCode = key })
                .ParseJsonAs<LoadSettingValuesResponse>();
        }
        public class LoadSettingValuesResponse
        {
            public Dictionary<string, string> SettingValues { get; set; }
        }

    }
}