using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Core.Module.Shared.Settings.KycUpdateFrequency
{
    public class KycUpdateFrequencyDataModel
    {
        public int DefaultMonthCount { get; set; }
        public const string DefaultValueName = "__default__";

        private static NTechCoreWebserviceException CreateException() => new NTechCoreWebserviceException($"Invalid kyc update frequency")
        {
            ErrorCode = "invalidKycUpdateFrequency",
            IsUserFacing = true,
            ErrorHttpStatusCode = 400
        };

        public static (int DefaultMonthCount, Dictionary<string, int> CustomMonthCounts) ParseSettingValues(Dictionary<string, string> settingValues)
        {
            var defaultValue = settingValues.Opt(DefaultValueName);

            if (defaultValue == null || !int.TryParse(defaultValue, out var defaultMonthCount))
            {
                throw CreateException();
            }

            var customMonthCounts = settingValues
                .Where(x => x.Key != DefaultValueName)
                .Select(x =>
                {
                    if (!int.TryParse(x.Value, out var monthCount))
                    {
                        throw CreateException();
                    };
                    return new { Name = x.Key, MonthCount = monthCount };
                })
                .ToDictionary(x => x.Name, x => x.MonthCount);

            return (DefaultMonthCount: defaultMonthCount, CustomMonthCounts: customMonthCounts);
        }

        public static Dictionary<string, string> ConvertToStoredSettingValues(int defaultMonthCount, Dictionary<string, int> customMonthCounts)
        {
            customMonthCounts = customMonthCounts ?? new Dictionary<string, int>();
            if (customMonthCounts.ContainsKey(DefaultValueName) || defaultMonthCount < 0 || customMonthCounts.Any(x => x.Value < 0))
            {
                throw CreateException();
            }
            var result = new Dictionary<string, string>(customMonthCounts.Count + 1);
            result[DefaultValueName] = defaultMonthCount.ToString();
            customMonthCounts.ToList().ForEach(x => result[x.Key] = x.Value.ToString());
            return result;
        }
    }
}
