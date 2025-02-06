using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NTech.Banking.BankAccounts;
using NTech.Core.Customer.Shared.Settings.BankAccount;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Settings.KycUpdateFrequency;
using System.Collections.Generic;
using System.Linq;
using static NTech.Core.Customer.Shared.Settings.SettingsModel.FormDataModel;

namespace NTech.Core.Customer.Shared.Settings
{
    public partial class SettingsModel
    {
        public List<Setting> Settings { get; set; }
        public Dictionary<string, string> UiGroupDisplayNames { get; set; }

        public class Setting
        {
            public string Code { get; set; }
            public string DisplayName { get; set; }
            public string Type { get; set; }
            public FeaturesModel Features { get; set; }
            public GroupsModel Groups { get; set; }
            public ComponentDataModel ComponentData { get; set; }
            public FormDataModel FormData { get; set; }
            public HtmlTemplateDataModel HtmlTemplateData { get; set; }
            public BankAccountSettingModel BankAccountData { get; set; }
            public KycUpdateFrequencyDataModel KycUpdateFrequencyData { get; set; }
            public string UiGroupName { get; set; }

            public bool IsFormSetting() => Type == SettingTypeCode.Form.ToString();
            public bool IsComponentSetting() => Type == SettingTypeCode.Component.ToString();
            public bool IsHtmlTemplateSetting() => Type == SettingTypeCode.HtmlTemplate.ToString();
            public bool IsAddRemoveRowsSetting() => Type == SettingTypeCode.AddRemoveRows.ToString();
            public bool IsBankAccountSetting() => Type == SettingTypeCode.BankAccount.ToString();
            public bool IsKycUpdateFrequencySetting() => Type == SettingTypeCode.KycUpdateFrequency.ToString();
            
            public Setting RequireFeaturesAny(params string[] featureNames)
            {
                foreach (var featureName in featureNames)
                {
                    Features.RequireAny.Add(featureName);
                }
                return this;
            }

            public Setting RequireFeaturesAll(params string[] featureNames)
            {
                foreach (var featureName in featureNames)
                {
                    Features.RequireAll.Add(featureName);
                }
                return this;
            }

            public Setting RequireGroupsAny(params string[] groupNames)
            {
                foreach (var groupName in groupNames)
                {
                    Groups.RequireAny.Add(groupName);
                }
                return this;
            }

            public bool IsAllowedToEdit(Setting setting, bool isSystemUser, HashSet<string> userGroupMemberships, IClientConfigurationCore clientConfiguration)
            {
                if (isSystemUser)
                    return true;

                if (setting?.Features?.RequireAny?.Count > 0 && !setting.Features.RequireAny.Any(clientConfiguration.IsFeatureEnabled))
                    return false;

                if (setting?.Features?.RequireAll?.Count > 0 && !setting.Features.RequireAll.All(clientConfiguration.IsFeatureEnabled))
                    return false;

                if (setting?.Groups?.RequireAny?.Count > 0 && userGroupMemberships.Intersect(setting.Groups.RequireAny).Count() == 0)
                    return false;

                return true;
            }

            public Setting IsPartOfUiGroup(string uiGroupName)
            {
                UiGroupName = uiGroupName;

                return this;
            }
        }

        public void SetUiGroupDisplayName(string uiGroupName, string uiGroupDisplayName)
        {
            UiGroupDisplayNames[uiGroupName] = uiGroupDisplayName;
        }

        public Setting AddComponentSetting(string code, string displayName, string componentName)
        {
            var s = new Setting
            {
                Code = code,
                DisplayName = displayName,
                Type = SettingTypeCode.Component.ToString(),
                Features = new FeaturesModel
                {
                    RequireAny = new HashSet<string>(),
                    RequireAll = new HashSet<string>()
                },
                Groups = new GroupsModel
                {
                    RequireAny = new HashSet<string>()
                },
                ComponentData = new ComponentDataModel
                {
                    ComponentName = componentName
                }
            };
            Settings.Add(s);
            return s;
        }

        public Setting AddFormSetting(string code, string displayName)
        {
            var s = new Setting
            {
                Code = code,
                DisplayName = displayName,
                Type = SettingTypeCode.Form.ToString(),
                Features = new FeaturesModel
                {
                    RequireAny = new HashSet<string>(),
                    RequireAll = new HashSet<string>()
                },
                Groups = new GroupsModel
                {
                    RequireAny = new HashSet<string>()
                },
                FormData = new FormDataModel
                {
                    Fields = new List<FormDataModel.FieldModel>()
                }
            };
            Settings.Add(s);
            return s;
        }

        public Setting AddHtmlTemplateSetting(string code, string displayName, string value)
        {
            var s = new Setting
            {
                Code = code,
                DisplayName = displayName,
                Type = SettingTypeCode.HtmlTemplate.ToString(),
                Features = new FeaturesModel
                {
                    RequireAny = new HashSet<string>(),
                    RequireAll = new HashSet<string>()
                },
                Groups = new GroupsModel
                {
                    RequireAny = new HashSet<string>()
                },
                HtmlTemplateData = new HtmlTemplateDataModel
                {
                    DefaultValue = value
                }
            };
            Settings.Add(s);
            return s;
        }

        public Setting AddAddRemoveRowsSetting(string code, string displayName)
        {
            var s = new Setting
            {
                Code = code,
                DisplayName = displayName,
                Type = SettingTypeCode.AddRemoveRows.ToString(),
                Features = new FeaturesModel
                {
                    RequireAny = new HashSet<string>(),
                    RequireAll = new HashSet<string>()
                },
                Groups = new GroupsModel
                {
                    RequireAny = new HashSet<string>()
                },
            };
            Settings.Add(s);
            return s;
        }

        /// <param name="isInitiallyEnabled">true/false to enable/disable, null to not have an option at all but rather having it be implicity enabled always.</param>
        /// <param name="defaultValue">If included must use the same format as </param>
        /// <returns></returns>
        public Setting AddBankAccountSetting(string code, string displayName, bool? isInitiallyEnabled,
            DefaultValue defaultValue = null,
            ISet<BankAccountNumberTypeCode> excludedAccountNrTypes = null,
            ISet<BankAccountNumberTypeCode> onlyTheseBankAccountNrTypes = null)
        {
            var s = new Setting
            {
                Code = code,
                DisplayName = displayName,
                Type = SettingTypeCode.BankAccount.ToString(),
                Features = new FeaturesModel
                {
                    RequireAny = new HashSet<string>(),
                    RequireAll = new HashSet<string>()
                },
                Groups = new GroupsModel
                {
                    RequireAny = new HashSet<string>()
                },
                BankAccountData = new BankAccountSettingModel
                {
                    DefaultBankAccountNr = defaultValue ?? new DefaultValue { Source = SettingsDefaultValueSourceCode.StaticValue, Value = "none" },
                    IsInitiallyEnabled = isInitiallyEnabled,
                    ExcludedAccountNrTypes = excludedAccountNrTypes,
                    OnlyTheseBankAccountNrTypes = onlyTheseBankAccountNrTypes
                }
            };
            Settings.Add(s);
            return s;
        }

        public class FeaturesModel
        {
            public HashSet<string> RequireAny { get; set; }
            public HashSet<string> RequireAll { get; set; }
        }
        public class GroupsModel
        {
            public HashSet<string> RequireAny { get; set; }
        }
        public class ComponentDataModel
        {
            public string ComponentName { get; set; }
        }

        public class HtmlTemplateDataModel
        {
            public string DefaultValue { get; set; }
        }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum SettingsDefaultValueSourceCode
    {
        ClientConfiguration,
        StaticValue
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum SettingTypeCode
    {
        Form,
        Component,
        HtmlTemplate,
        AddRemoveRows,
        BankAccount,
        KycUpdateFrequency
    }
}
