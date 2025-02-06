using System;

namespace nPreCredit.Code.AffiliateReporting
{
    public class AffiliateCallbackThrottlingPolicy : IAffiliateCallbackThrottlingPolicy
    {
        public const string StandardContextName = "standard";

        private readonly string providerName;
        private readonly string context;
        private readonly TimeSpan throttlingWindow;
        private readonly IThrottlingPolicyDataSource throttlingPolicyDataSource;
        private readonly int throttlingCount;

        public AffiliateCallbackThrottlingPolicy(string providerName, string context, TimeSpan throttlingWindow, int throttlingCount, IThrottlingPolicyDataSource throttlingPolicyDataSource)
        {
            this.providerName = providerName;
            this.context = context;
            this.throttlingWindow = throttlingWindow;
            this.throttlingPolicyDataSource = throttlingPolicyDataSource;
            this.throttlingCount = throttlingCount;
        }

        public bool IsThrottled(string providerName, string context)
        {
            if (this.providerName != providerName || this.context != context)
                return false;

            var toDate = this.throttlingPolicyDataSource.Now;
            var fromDate = toDate.Subtract(this.throttlingWindow);

            var callCount = this.throttlingPolicyDataSource.GetCallCount(providerName, context, fromDate, toDate);

            return callCount >= this.throttlingCount;
        }
    }
}