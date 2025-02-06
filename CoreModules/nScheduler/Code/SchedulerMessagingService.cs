using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace nScheduler.Code
{
    public interface ISchedulerMessagingService
    {
        void SendAfterRunAlertMessage(SchedulerAfterRunAlertMessageContext context);
    }

    public class SchedulerAfterRunAlertMessageContext
    {
        public string TimeslotName { get; set; }
        public string TimeslotStartTime { get; set; }
        public IList<Item> TimeslotItems { get; set; }
        public class Item
        {
            public string Name { get; set; }
            public string Status { get; set; }
            public bool HasWarnings { get; set; }
            public IList<WarningItem> Warnings { get; set; }
        }

        public class WarningItem
        {
            public string Text { get; set; }
        }

        public void AddItem(string name, string status, IList<string> warnings)
        {
            if (TimeslotItems == null)
                TimeslotItems = new List<Item>();
            TimeslotItems.Add(new Item
            {
                HasWarnings = warnings != null && warnings.Count > 0,
                Warnings = warnings?.Select(x => new WarningItem { Text = x })?.ToList(),
                Name = name,
                Status = status
            });
        }
    }

    public class EmailSchedulerMessagingService : ISchedulerMessagingService
    {
        public void SendAfterRunAlertMessage(SchedulerAfterRunAlertMessageContext context)
        {
            var mines = (JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(context), new ExpandoObjectConverter()) as IDictionary<string, object>)?.ToDictionary(x => x.Key, x => x.Value);
            var e = Email.EmailServiceFactory.CreateEmailService();
            e.SendTemplateEmailComplex(NEnv.SchedulerAlertEmail, "scheduler-alert", mines, "scheduler-alert");
        }
    }

    public class DoNothingMessagingProvider : ISchedulerMessagingService
    {
        public void SendAfterRunAlertMessage(SchedulerAfterRunAlertMessageContext context)
        {

        }
    }

    public class SchedulerMessagingFactory
    {
        public static ISchedulerMessagingService CreateInstance()
        {
            var provider = NEnv.SchedulerAlertProvider.ToLowerInvariant();
            if (provider == "email")
                return new EmailSchedulerMessagingService();
            else if (provider == "none")
                return new DoNothingMessagingProvider();
            else
                throw new NotImplementedException();
        }
    }
}