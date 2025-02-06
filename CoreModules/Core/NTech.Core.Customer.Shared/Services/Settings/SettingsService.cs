using Newtonsoft.Json;
using NTech.Core.Customer.Shared.Settings;
using NTech.Core.Customer.Shared.Settings.BankAccount;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Core.Module.Shared.Settings.KycUpdateFrequency;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static NTech.Core.Customer.Shared.Settings.SettingsModel;

namespace nCustomer.Code.Services.Settings
{
    public class SettingsService : ReadonlySettingsService
    {
        private readonly INTechCurrentUserMetadata user;
        private readonly Action<string, string> broadcastEvent;

        public SettingsService(SettingsModelSource settingsModelSource, IKeyValueStoreService keyValueStore, INTechCurrentUserMetadata user, IClientConfigurationCore clientConfiguration, Action<string, string> broadcastEvent) : base(settingsModelSource, keyValueStore, clientConfiguration)
        {
            this.user = user;
            this.broadcastEvent = broadcastEvent;
        }

        public void SaveSettingsValues(string settingCode, Dictionary<string, string> newValues, (bool IsSystemUser, HashSet<string> GroupMemberships) user)
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

            if (!setting.IsAllowedToEdit(setting, user.IsSystemUser, user.GroupMemberships, clientConfiguration))
                throw new NTechCoreWebserviceException("Not permitted to edit setting")
                {
                    ErrorCode = "permissonDenied",
                    IsUserFacing = true,
                    ErrorHttpStatusCode = 400
                };

            var valuesToSave = new Dictionary<string, string>();

            if (setting.IsFormSetting())
            {
                newValues = newValues ?? new Dictionary<string, string>();

                valuesToSave = ParseValuesToSave(setting.FormData.Fields, newValues);
            }
            else if (setting.IsHtmlTemplateSetting())
            {
                valuesToSave = newValues;
            }
            else if (setting.IsAddRemoveRowsSetting())
            {
                valuesToSave = newValues;
            }
            else if (setting.IsBankAccountSetting())
            {
                valuesToSave = BankAccountSettingModel.TransformUiSaveValuesToStoredSettingValues(setting.BankAccountData, newValues);
            }
            else if (setting.IsKycUpdateFrequencySetting())
            {
                var parsedSetting = KycUpdateFrequencyDataModel.ParseSettingValues(newValues);
                valuesToSave = KycUpdateFrequencyDataModel.ConvertToStoredSettingValues(parsedSetting.DefaultMonthCount, parsedSetting.CustomMonthCounts);
            }
            else
                throw new NTechCoreWebserviceException("This settingtype exposes no values")
                {
                    ErrorCode = "settingTypeHasNoValues",
                    IsUserFacing = true,
                    ErrorHttpStatusCode = 400
                };

            if (valuesToSave.Count > 0)
            {
                var validationErrors = settingsModelSource.ValidateOnSave(settingCode, valuesToSave);
                if(validationErrors?.Count > 0)
                    throw new NTechCoreWebserviceException(string.Join(Environment.NewLine, validationErrors))
                    {
                        ErrorCode = "saveSettingValuesValidationError",
                        IsUserFacing = true,
                        ErrorHttpStatusCode = 400
                    };

                if (!TrySaveUserSettingValues(settingCode, valuesToSave))
                    throw new NTechCoreWebserviceException("Could not save setting values")
                    {
                        ErrorCode = "failedToSaveSettingValues",
                        IsUserFacing = true,
                        ErrorHttpStatusCode = 400
                    };
            }
        }

        public bool ClearUserValues(string settingCode)
        {
            bool wasRemoved = false;
            keyValueStore.RemoveValue(settingCode, KeySpace, x => wasRemoved = x);

            if (wasRemoved)
                BroadCastSettingsChangedEvent(settingCode);

            return wasRemoved;
        }

        private Dictionary<string, string> ParseValuesToSave(List<SettingsModel.FormDataModel.FieldModel> existingValues, Dictionary<string, string> newValues)
        {
            var valuesToSave = new Dictionary<string, string>();

            foreach (var field in existingValues.Where(x => newValues.ContainsKey(x.Name)))
            {
                if (field.IsInterestRateField())
                {
                    var rawValue = newValues[field.Name];
                    if (string.IsNullOrWhiteSpace(rawValue) || !decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedValue) || parsedValue < 0)
                        throw new NTechCoreWebserviceException($"{field.Name}: Invalid interest rate")
                        {
                            ErrorCode = "invalidInterestRate",
                            IsUserFacing = true,
                            ErrorHttpStatusCode = 400
                        };
                    valuesToSave[field.Name] = Math.Round(parsedValue, 4).ToString(CultureInfo.InvariantCulture);
                }
                else if (field.IsEnumField())
                {
                    var rawValue = newValues[field.Name];
                    if (string.IsNullOrWhiteSpace(rawValue) || !field.EnumOptions.Any(x => x.Code == rawValue))
                        throw new NTechCoreWebserviceException($"{field.Name}: Invalid enum value")
                        {
                            ErrorCode = "invalidEnumValue",
                            IsUserFacing = true,
                            ErrorHttpStatusCode = 400
                        };
                    valuesToSave[field.Name] = rawValue;
                }
                else if (field.IsPositiveIntegerField())
                {
                    var rawValue = newValues[field.Name];
                    if (string.IsNullOrWhiteSpace(rawValue) || !int.TryParse(rawValue, out var parsedValue) || parsedValue < 0)
                        throw new NTechCoreWebserviceException($"{field.Name}: Invalid positive integer value")
                        {
                            ErrorCode = "invalidPositiveIntegerValue",
                            IsUserFacing = true,
                            ErrorHttpStatusCode = 400
                        };
                    valuesToSave[field.Name] = rawValue;
                }
                else if (field.IsUrlField())
                {
                    var rawValue = newValues[field.Name];
                    if (string.IsNullOrWhiteSpace(rawValue) || !Uri.IsWellFormedUriString(rawValue, UriKind.Absolute))
                        throw new NTechCoreWebserviceException($"{field.Name}: Invalid url value")
                        {
                            ErrorCode = "invalidUrlValue",
                            IsUserFacing = true,
                            ErrorHttpStatusCode = 400
                        };
                    valuesToSave[field.Name] = rawValue;
                }
                else if (field.IsHiddenTextField() || field.IsTextAreaField() || field.IsTextField())
                {
                    // TODO: implement validation that sets default value if null
                    // Without validation, swallow whatever is added. 
                    var rawValue = newValues[field.Name];
                    valuesToSave[field.Name] = rawValue;
                }
                else
                    throw new NotImplementedException();
            }

            return valuesToSave;
        }

        private bool TrySaveUserSettingValues(string settingCode, Dictionary<string, string> values)
        {
            if (values == null || values.Count == 0)
                return false;

            keyValueStore.SetValue(settingCode, KeySpace, JsonConvert.SerializeObject(values), user);

            BroadCastSettingsChangedEvent(settingCode);

            return true;
        }

        private void BroadCastSettingsChangedEvent(string settingsCode)
        {
            CachedSettingsService.OnSettingChanged(settingsCode);
            broadcastEvent("SettingChanged", settingsCode);
        }
    }
}