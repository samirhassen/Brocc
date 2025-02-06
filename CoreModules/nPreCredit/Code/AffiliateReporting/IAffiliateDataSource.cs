namespace nPreCredit.Code.AffiliateReporting
{
    public interface IAffiliateDataSource
    {
        IAffiliateCallbackThrottlingPolicy GetThrottlingPolicy(string providerName);
        IAffiliateCallbackDispatcher GetDispatcher(string providerName);
        AffiliateCallbackSettingsModel GetSettings(string providerName);
    }
}