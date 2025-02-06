using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace nPreCredit.Code.AffiliateReporting
{
    public class FileSystemAffiliateDataSource : IAffiliateDataSource
    {
        private readonly IThrottlingPolicyDataSource throttlingPolicyDataSource;
        private readonly IAffiliateCallbackDispatcherFactory affiliateCallbackDispatcherFactory;

        public FileSystemAffiliateDataSource(IThrottlingPolicyDataSource throttlingPolicyDataSource, IAffiliateCallbackDispatcherFactory affiliateCallbackDispatcherFactory)
        {
            this.throttlingPolicyDataSource = throttlingPolicyDataSource;
            this.affiliateCallbackDispatcherFactory = affiliateCallbackDispatcherFactory;
        }

        public IAffiliateCallbackDispatcher GetDispatcher(string providerName)
        {
            var settings = GetSettings(providerName);
            if (settings == null || string.IsNullOrWhiteSpace(settings.DispatcherName))
                return null;
            return affiliateCallbackDispatcherFactory.GetDispatcher(settings.DispatcherName);
        }

        public IAffiliateCallbackThrottlingPolicy GetThrottlingPolicy(string providerName)
        {
            var settings = GetSettings(providerName);
            if (settings == null)
                return null;
            if (settings.ThrottlingWindows == null || settings.ThrottlingWindows.Count == 0)
                return null;

            var ps = new List<IAffiliateCallbackThrottlingPolicy>();
            foreach (var w in settings.ThrottlingWindows)
            {
                ps.Add(new AffiliateCallbackThrottlingPolicy(
                    providerName,
                    "standard",
                    w.MilliSeconds.HasValue
                        ? TimeSpan.FromMilliseconds(w.MilliSeconds.Value)
                        : w.Seconds.HasValue
                            ? TimeSpan.FromSeconds(w.Seconds.Value)
                            : w.Minutes.HasValue
                                ? TimeSpan.FromMinutes(w.Minutes.Value)
                                : w.Hours.HasValue
                                    ? TimeSpan.FromHours(w.Hours.Value)
                                    : TimeSpan.FromSeconds(1),
                    w.Count ?? 1,
                    throttlingPolicyDataSource));
            }
            return new CompositeAffiliateCallbackThrottlingPolicy(ps.ToArray());
        }

        public AffiliateCallbackSettingsModel GetSettings(string providerName)
        {
            if (NEnv.GetAffiliateModel(providerName, allowMissing: true)?.IsSelf ?? false)
                return new AffiliateCallbackSettingsModel
                {
                    DispatcherName = "self",
                    CustomSettings = new Dictionary<string, string>(),
                    ThrottlingWindows = new List<AffiliateCallbackSettingsModel.ThrottlingWindow>
                    {
                        new AffiliateCallbackSettingsModel.ThrottlingWindow { Count = 1, Seconds = 1 }
                    }
                };

            var file = Path.Combine(NEnv.AffiliateReportingSourceFolder.FullName, $"{providerName}-callback-settings.json");
            if (!File.Exists(file))
                return null;
            return JsonConvert.DeserializeObject<AffiliateCallbackSettingsModel>(File.ReadAllText(file));
        }
    }
}