namespace nPreCredit.Code.AffiliateReporting
{
    public class CompositeAffiliateCallbackThrottlingPolicy : IAffiliateCallbackThrottlingPolicy
    {
        private readonly IAffiliateCallbackThrottlingPolicy[] policies;

        public CompositeAffiliateCallbackThrottlingPolicy(params IAffiliateCallbackThrottlingPolicy[] policies)
        {
            this.policies = policies;
        }

        public bool IsThrottled(string providerName, string context)
        {
            foreach (var p in policies)
            {
                if (p.IsThrottled(providerName, context))
                    return true;
            }
            return false;
        }
    }
}