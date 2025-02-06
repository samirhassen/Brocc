namespace NTech.Core
{
    [AttributeUsage(AttributeTargets.Class)]
    public class NTechRequireFeaturesAttribute : Attribute
    {
        public string[] RequireFeaturesAny { set; get; }
        public string[] RequireFeaturesAll { set; get; }
        public string[] RequireClientCountryAny { set; get; }

        public bool IsEnabled(Func<string, bool> isFeatureEnabled, string clientCountryIsoCode)
        {
            if (RequireFeaturesAny != null && RequireFeaturesAny.Length > 0)
            {
                if (!RequireFeaturesAny.Any(isFeatureEnabled))
                    return false;
            }

            if (RequireFeaturesAll != null && RequireFeaturesAll.Length > 0)
            {
                if (!RequireFeaturesAll.All(isFeatureEnabled))
                    return false;
            }

            if (RequireClientCountryAny != null && RequireClientCountryAny.Length > 0)
            {
                if (!RequireClientCountryAny.Contains(clientCountryIsoCode, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
