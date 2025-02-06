using System.Threading;

namespace nPreCredit.Code.AffiliateReporting
{

    public interface IAffiliateEventProcessor
    {
        void ProcessIncomingEvents(CancellationToken cancellationToken);
    }
}