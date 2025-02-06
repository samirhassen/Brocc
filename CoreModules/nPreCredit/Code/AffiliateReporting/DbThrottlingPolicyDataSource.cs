using System;
using System.Linq;

namespace nPreCredit.Code.AffiliateReporting
{
    public class DbThrottlingPolicyDataSource : IThrottlingPolicyDataSource
    {
        public DateTime Now => DateTime.Now; //NOTE: Dont use IClock here since we want time to flow even in test

        public int GetCallCount(string providerName, string context, DateTime fromDate, DateTime toDate)
        {
            using (var dbContext = new PreCreditContext())
            {
                return dbContext
                    .AffiliateReportingLogItems
                    .Where(x => x.ProviderName == providerName && x.ThrottlingContext == context && x.LogDate >= fromDate && x.LogDate <= toDate)
                    .Sum(x => (int?)x.ThrottlingCount) ?? 0;
            }
        }
    }
}