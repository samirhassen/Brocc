namespace nPreCredit.Code.AffiliateReporting
{

    public interface IAffiliateCallbackThrottlingPolicy
    {
        bool IsThrottled(string providerName, string context);

    }
}