using NTech.Core.PreCredit.Shared.Services;

namespace nPreCredit.Code.Services
{
    public class PublishEventService : IPublishEventService
    {
        public void Publish(PreCreditEventCode eventCode, string data)
        {
            Publish(eventCode.ToString(), data);
        }

        public void Publish(string eventCode, string data)
        {
            NTech.Services.Infrastructure.Eventing.NTechEventHandler.PublishEvent(eventCode, data);
        }
    }
}