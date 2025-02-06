using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;

namespace nCreditReport.Code
{
    public class nPreCreditClient
    {
        protected NHttp.NHttpCall Begin()
        {
            return NHttp.Begin(new Uri(NEnv.ServiceRegistry.Internal["nPreCredit"]), NHttp.GetCurrentAccessToken());
        }


        public List<int> FilterOutCustomersWithInactiveApplications(List<int> customerIds, int minNumberOfDaysInactive)
        {
            return Begin()
               .PostJson("Api/ApplicationCustomerList/Filter-Out-Customers-With-Inactive-Applications", new { customerIds, minNumberOfDaysInactive })
               .ParseJsonAsAnonymousType(new { ArchivableCustomerIds = (List<int>)null })
            ?.ArchivableCustomerIds;
        }
    }
}