using nPreCredit.DbModel;

namespace nPreCredit.Code.AffiliateReporting
{

    public interface IAffiliateCallbackDispatcher
    {
        HandleEventResult Dispatch(AffiliateReportingEvent evt);
    }
}