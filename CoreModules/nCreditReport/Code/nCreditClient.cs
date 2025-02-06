using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;

namespace nCreditReport.Code
{
    public class nCreditClient
    {
        protected NHttp.NHttpCall Begin()
        {
            return NHttp.Begin(new Uri(NEnv.ServiceRegistry.Internal["nCredit"]), NHttp.GetCurrentAccessToken());
        }


        public List<int> FilterOutCustomersWithLoans(List<int> customerIds)
        {
            return Begin()
               .PostJson("Api/Credit/Filter-Out-Customers-With-Loans", new { customerIds })
               .ParseJsonAsAnonymousType(new { ArchivableCustomerIds = (List<int>)null })
            ?.ArchivableCustomerIds;
        }
    }
}