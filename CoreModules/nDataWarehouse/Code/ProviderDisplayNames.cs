using System;
using System.Collections.Generic;
using System.Linq;

namespace nDataWarehouse
{
    public static class ProviderDisplayNames
    {
        private static Lazy<Dictionary<string, string>> providerDisplayNames = new Lazy<Dictionary<string, string>>(
            () => NEnv.GetAffiliateModels().ToDictionary(x => x.ProviderName, x => x.DisplayToEnduserName));

        public static string GetProviderDisplayName(string providerName)
        {
            if (string.IsNullOrWhiteSpace(providerName) || !providerDisplayNames.Value.ContainsKey(providerName))
                return providerName;
            return providerDisplayNames.Value[providerName];
        }

        public class AffiliateModelPartial
        {
            public string ProviderName { get; set; }
            public string DisplayToEnduserName { get; set; }
        }
    }
}