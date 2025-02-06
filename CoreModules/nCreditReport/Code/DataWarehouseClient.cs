using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;

namespace nCreditReport.Code
{
    public class DataWarehouseClient
    {
        protected NHttp.NHttpCall Begin()
        {
            return NHttp.Begin(new Uri(NEnv.ServiceRegistry.Internal["nDataWarehouse"]), NHttp.GetCurrentAccessToken());
        }

        public void MergeDimension<T>(string dimensionName, List<T> values)
        {
            Begin()
                .PostJson("Api/MergeDimension", new { dimensionName = dimensionName, values = values })
                .EnsureSuccessStatusCode();
        }

        public void MergeFact<T>(string factName, List<T> values)
        {
            Begin()
                .PostJson("Api/MergeFact", new { factName = factName, values = values })
                .EnsureSuccessStatusCode();
        }
    }
}