using System;

namespace nScheduler
{
    public class ServiceRun : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string TimeSlotName { get; set; }
        public string JobName { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string EndStatus { get; set; }
        public string EndStatusData { get; set; }
        public long? RuntimeInMs { get; set; }
        public int TriggeredById { get; set; }
    }
}