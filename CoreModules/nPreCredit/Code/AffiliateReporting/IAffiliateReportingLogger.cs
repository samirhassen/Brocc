namespace nPreCredit.Code.AffiliateReporting
{
    public interface IAffiliateReportingLogger
    {
        void Log(long incomingApplicationEventId, string providerName, HandleEventResult result);
    }
}