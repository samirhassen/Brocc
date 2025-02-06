namespace nPreCredit.Code.AffiliateReporting
{
    public interface IAffiliateCallbackDispatcherFactory
    {
        IAffiliateCallbackDispatcher GetDispatcher(string dispatcherName);
    }
}
