using System;

namespace nPreCredit.Code.AffiliateReporting
{
    public interface IThrottlingPolicyDataSource
    {
        DateTime Now { get; }
        int GetCallCount(string providerName, string context, DateTime fromDate, DateTime toDate);
    }
}