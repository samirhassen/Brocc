namespace NTech.Core.Customer.Shared.Services
{
    public interface ICrossServiceEventService
    {
        void BroadcastCrossServiceEvent(string eventName, string data);
    }
}
