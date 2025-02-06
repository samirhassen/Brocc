using Newtonsoft.Json;
using NTech.Core.Customer.Shared.Settings;
using NTech.Core.Customer.Shared.Settings.BankAccount;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Settings.KycUpdateFrequency;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer.Code.Services.Settings
{
    public class ReadonlySettingsService
    {
        protected readonly SettingsModelSource settingsModelSource;
        protected readonly IKeyValueStoreService keyValueStore;
        protected readonly IClientConfigurationCore clientConfiguration;

        public ReadonlySettingsService(SettingsModelSource settingsModelSource, IKeyValueStoreService keyValueStore, IClientConfigurationCore clientConfiguration)
        {
            this.settingsModelSource = settingsModelSource;
            this.keyValueStore = keyValueStore;
            this.clientConfiguration = clientConfiguration;
        }

        public Dictionary<string, string> LoadSettingsValues(string settingCode)
        {
            var model = settingsModelSource.GetSettingsModel();
            var setting = model.Settings.SingleOrDefault(x => x.Code == settingCode);
            if (setting == null)
                throw new NTechCoreWebserviceException("Invalid settingCode")
                {
                    ErrorCode = "invalidSettingCode",
                    IsUserFacing = true,
                    ErrorHttpStatusCode = 400
                };

            string GetClientConfigValue(string path) => clientConfiguration.GetSingleCustomValue(true, path.Split('.'));

            Dictionary<string, string> result;
            if (setting.IsFormSetting())
            {
                result = new Dictionary<string, string>();
                var userValues = LoadUserSettingValues(settingCode) ?? new Dictionary<string, string>();
                foreach (var field in setting.FormData.Fields)
                {
                    if (userValues.ContainsKey(field.Name))
                        result[field.Name] = userValues[field.Name];
                    else if (field.HasClientConfigurationDefaultValue())
                        result[field.Name] = GetClientConfigValue(field.DefaultValuePath);
                    else if (field.HasStaticDefaultValue())
                        result[field.Name] = field.StaticValue;
                    else
                        throw new NotImplementedException();
                }
            }
            else if (setting.IsHtmlTemplateSetting())
            {
                result = new Dictionary<string, string>();
                var userValues = LoadUserSettingValues(settingCode) ?? new Dictionary<string, string>();
                if (userValues.ContainsKey(settingCode))
                    result[settingCode] = userValues[settingCode];
                else
                    result[settingCode] = setting.HtmlTemplateData.DefaultValue;
            }
            else if (setting.IsAddRemoveRowsSetting())
            {
                return LoadUserSettingValues(settingCode) ?? new Dictionary<string, string>
                {
                    ["listOfNames"] = JsonConvert.SerializeObject(new List<string>())
                };
            }
            else if (setting.IsBankAccountSetting())
            {
                var userValues = LoadUserSettingValues(settingCode) ?? new Dictionary<string, string>();
                return BankAccountSettingModel.PopulateSettingOnLoad(setting.BankAccountData, userValues, GetClientConfigValue);
            }
            else if (setting.IsKycUpdateFrequencySetting())
            {
                return LoadUserSettingValues(settingCode) ?? new Dictionary<string, string>
                {
                    [KycUpdateFrequencyDataModel.DefaultValueName] = setting.KycUpdateFrequencyData.DefaultMonthCount.ToString()
                };
            }
            else
                result = new Dictionary<string, string>();

            return result;
        }

        public const string KeySpace = "SettingsV1";
        /// <summary>
        /// Note: This loads just user overrides so if you want to use settings from nCustomer in the same
        /// way it's used when other services call Api/Settings/LoadValues then you should use LoadSettingsValues instead.
        /// </summary>
        public Dictionary<string, string> LoadUserSettingValues(string settingCode)
        {
            var value = keyValueStore.GetValue(settingCode, KeySpace);
            return string.IsNullOrWhiteSpace(value) ? null : JsonConvert.DeserializeObject<Dictionary<string, string>>(value);
        }
    }
}
