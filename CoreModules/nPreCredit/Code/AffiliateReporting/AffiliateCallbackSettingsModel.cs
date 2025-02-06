using Newtonsoft.Json;
using System.Collections.Generic;

namespace nPreCredit.Code.AffiliateReporting
{
    public class AffiliateCallbackSettingsModel
    {
        public string DispatcherName { get; set; }
        public List<ThrottlingWindow> ThrottlingWindows { get; set; }
        public Dictionary<string, string> CustomSettings { get; set; }
        public class ThrottlingWindow
        {
            public int? Count { get; set; }
            public int? MilliSeconds { get; set; }
            public int? Seconds { get; set; }
            public int? Minutes { get; set; }
            public int? Hours { get; set; }
        }

        public T ReadCustomSettingsAs<T>()
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(CustomSettings));
        }
    }
}