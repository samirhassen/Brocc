using System;
using System.Collections.Generic;

namespace NTech.Core.Module.Shared.Infrastructure
{
    public interface IClientConfigurationCore
    {
        ClientConfigurationCoreCountry Country { get; }
        string ClientName { get; }
        bool IsFeatureEnabled(string featureName);
        string OptionalSetting(string settingName);
        int? GetSingleCustomInt(bool mustExist, params string[] elementPath);
        bool? GetSingleCustomBoolean(bool mustExist, params string[] elementPath);
        string GetSingleCustomValue(bool mustExist, params string[] elementPath);
        List<string> GetRepeatedCustomValue(params string[] elementPath);
    }

    public class ClientConfigurationCoreCountry
    {
        public string BaseCurrency { get; set; }
        public string BaseCountry { get; set; }
        public string BaseFormattingCulture { get; set; }

        public string GetBaseLanguage()
        {
            if (BaseCountry == "FI")
                return "fi";
            else if (BaseCountry == "SE")
                return "sv";
            else
                throw new NotImplementedException();
        }
    }
}
