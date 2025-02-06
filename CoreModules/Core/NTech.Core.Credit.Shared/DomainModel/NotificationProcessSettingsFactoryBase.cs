using nCredit.Code.MortgageLoans;
using nCredit.DomainModel;
using Newtonsoft.Json;
using NTech.Banking.Conversion;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.IO;


namespace nCredit.DbModel.DomainModel
{
    public abstract class NotificationProcessSettingsFactoryBase : INotificationProcessSettingsFactory
    {
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly CachedSettingsService cachedSettingsService;
        private readonly ICreditEnvSettings envSettings;
        private readonly string settingChangedCallbackId;

        protected static readonly Lazy<FewItemsCache> cache = new Lazy<FewItemsCache>();

        public NotificationProcessSettingsFactoryBase(IClientConfigurationCore clientConfiguration, CachedSettingsService cachedSettingsService, ICreditEnvSettings envSettings)
        {
            this.clientConfiguration = clientConfiguration;
            this.cachedSettingsService = cachedSettingsService;
            this.envSettings = envSettings;
            settingChangedCallbackId = CachedSettingsService.RegisterSettingChangedCallback(OnSettingChanged);
        }

        public NotificationProcessSettings GetByCreditType(string creditType)
            => GetByCreditType(Enums.ParseReq<CreditType>(creditType));

        private string GetCacheKey(CreditType creditType) => $"ee031b0a-0b75-47e3-93a5-4b5232250d5e.{creditType}.{settingChangedCallbackId}";

        public NotificationProcessSettings GetByCreditType(CreditType creditType)
        {
            NotificationProcessSettings CreateRawSettings()
            {
                switch (creditType)
                {
                    case CreditType.MortgageLoan: return CreateMortgageLoanNotificationProcessSettings();
                    case CreditType.UnsecuredLoan: return CreateUnsecuredLoanNotificationProcessSettings();
                    case CreditType.CompanyLoan: return CreateCompanyLoanNotificationProcessSettings();
                    default:
                        throw new NotImplementedException();
                }
            }

            return cache.Value.WithCache(GetCacheKey(creditType), TimeSpan.FromHours(1),
                () => OverrideNotificationProcessSettings(CreateRawSettings()));
        }

        private NotificationProcessSettings GetStandardNotificationProcessSettings()
        {
            var clientConfig = clientConfiguration;
            return new NotificationProcessSettings
            {
                NotificationNotificationDay = clientConfig.GetSingleCustomInt(true, "NotificationProcessSettings", "NotificationNotificationDay").Value,
                NotificationDueDay = clientConfig.GetSingleCustomInt(true, "NotificationProcessSettings", "NotificationDueDay").Value,
                PaymentFreeMonthMinNrOfPaidNotifications = clientConfig.GetSingleCustomInt(false, "NotificationProcessSettings", "PaymentFreeMonthMinNrOfPaidNotifications"),
                PaymentFreeMonthExcludeNotificationFee = clientConfig.GetSingleCustomBoolean(false, "NotificationProcessSettings", "PaymentFreeMonthExcludeNotificationFee") ?? false,
                PaymentFreeMonthMaxNrPerYear = clientConfig.GetSingleCustomInt(false, "NotificationProcessSettings", "PaymentFreeMonthMaxNrPerYear") ?? 0,
                MinMonthsBetweenPaymentFreeMonths = clientConfig.GetSingleCustomInt(false, "NotificationProcessSettings", "MinMonthsBetweenPaymentFreeMonths") ?? 0,
                ReminderFeeAmount = clientConfig.GetSingleCustomInt(true, "NotificationProcessSettings", "ReminderFeeAmount").Value,
                ReminderMinDaysBetween = clientConfig.GetSingleCustomInt(true, "NotificationProcessSettings", "ReminderMinDaysBetween").Value,
                SkipReminderLimitAmount = clientConfig.GetSingleCustomInt(true, "NotificationProcessSettings", "SkipReminderLimitAmount").Value,
                NotificationOverDueGraceDays = clientConfig.GetSingleCustomInt(true, "NotificationProcessSettings", "NotificationOverDueGraceDays").Value,
                MaxNrOfReminders = clientConfig.GetSingleCustomInt(true, "NotificationProcessSettings", "MaxNrOfReminders").Value,
                AreBackToBackPaymentFreeMonthsAllowed = clientConfig.GetSingleCustomBoolean(false, "NotificationProcessSettings", "AreBackToBackPaymentFreeMonthsAllowed") ?? false,
                MaxNrOfRemindersWithFees = clientConfig.GetSingleCustomInt(false, "NotificationProcessSettings", "MaxNrOfRemindersWithFees"),
                NrOfFreeInitialReminders = clientConfig.GetSingleCustomInt(false, "NotificationProcessSettings", "NrOfFreeInitialReminders") ?? 0,
                FirstReminderDaysBefore = clientConfig.GetSingleCustomInt(false, "NotificationProcessSettings", "FirstReminderDaysBefore") ?? NotificationProcessSettings.DefaultFirstReminderDaysBefore,                
            };
        }

        protected abstract string GetAppSetting(string name, bool mustExist);
        protected abstract FileInfo GetClientResourceFile(string settingName, string resourceFolderRelativePath, bool mustExist);

        private NotificationProcessSettings CreateUnsecuredLoanNotificationProcessSettings()
        {
            if (!envSettings.IsUnsecuredLoansEnabled)
                throw new Exception("Unsecured loans not enabled");

            T? OptT<T>(string n, Func<string, T> parse) where T : struct
            {
                var v = GetAppSetting(n, false);
                if (v == null)
                    return new T?();
                else
                    return parse(v);
            }

            if (envSettings.IsStandardUnsecuredLoansEnabled)
            {
                return GetStandardNotificationProcessSettings();
            }
            else
            {
                //TODO: Stick these in the client config for existing clients also and remove this part
                return new NotificationProcessSettings
                {
                    NotificationNotificationDay = 14,
                    NotificationDueDay = 28,
                    PaymentFreeMonthMinNrOfPaidNotifications = OptT("ntech.credit.paymentfreemonth.minnrpaidnotifications", int.Parse) ?? 3,
                    PaymentFreeMonthExcludeNotificationFee = OptT("ntech.credit.paymentfreemonth.excludenotificationfee", s => s.ToLowerInvariant().Trim() == "true") ?? false,
                    PaymentFreeMonthMaxNrPerYear = OptT("ntech.credit.paymentfreemonth.maxnrperyear", int.Parse) ?? 3,
                    ReminderFeeAmount = OptT("ntech.credit.reminderfeeamount", int.Parse) ?? 5,
                    ReminderMinDaysBetween = OptT("ntech.credit.remindermindaysbetween", int.Parse) ?? 7,
                    SkipReminderLimitAmount = OptT("ntech.credit.reminderskipbelowlimitamount", int.Parse) ?? 10,
                    NotificationOverDueGraceDays = OptT("ntech.credit.notificationoverduegracedays", int.Parse) ?? 5,
                    MaxNrOfReminders = OptT("ntech.credit.maxnrofreminders", int.Parse) ?? 2,
                    AreBackToBackPaymentFreeMonthsAllowed = OptT("ntech.feature.credit.allowbacktobackpaymentfreemonths", s => s.ToLowerInvariant().Trim() == "true") ?? clientConfiguration.IsFeatureEnabled("ntech.feature.credit.allowbacktobackpaymentfreemonths"),
                    MinMonthsBetweenPaymentFreeMonths = OptT("ntech.credit.minmonthsbetweenpaymentfreemonths", int.Parse) ?? 3,
                    FirstReminderDaysBefore = OptT("ntech.credit.firstReminderDaysBefore", int.Parse) ?? NotificationProcessSettings.DefaultFirstReminderDaysBefore
                };
            }
        }

        private NotificationProcessSettings CreateCompanyLoanNotificationProcessSettings()
        {
            if (!envSettings.IsCompanyLoansEnabled)
                throw new Exception("Company loans not enabled");

            var f = GetClientResourceFile("ntech.credit.companyloan.notificationsettingsfile", "CompanyLoan-NotificationSettings.json", true);
            var result = JsonConvert.DeserializeObject<NotificationProcessSettings>(File.ReadAllText(f.FullName));
            if (result != null)
            {
                result.FirstReminderDaysBefore = result.FirstReminderDaysBefore ?? NotificationProcessSettings.DefaultFirstReminderDaysBefore;
            }
            return result;
        }

        private NotificationProcessSettings CreateMortgageLoanNotificationProcessSettings()
        {
            if (!envSettings.IsMortgageLoansEnabled)
                throw new Exception("Mortage loans not enabled");

            if (envSettings.IsStandardMortgageLoansEnabled)
            {
                return GetStandardNotificationProcessSettings();
            }
            else
            {
                var f = GetClientResourceFile("ntech.credit.mortgageloan.notificationsettingsfile", "MortgageLoan-NotificationSettings.json", true);
                return ConvertMortgageLoanNotificationSettingsModelToNotificationProcessSettings(JsonConvert.DeserializeObject<MortgageLoanNotificationSettingsModel>(File.ReadAllText(f.FullName)));
            }
        }

        private void OnSettingChanged(string settingCode)
        {
            cache.Value.ClearCache();
        }

        private NotificationProcessSettings OverrideNotificationProcessSettings(NotificationProcessSettings settings)
        {
            var reminderSettings = cachedSettingsService.LoadSettings("reminderFeeSettings");
            if (reminderSettings.OptVal("isEnabled") != "true")
                return settings;

            var skipReminderLimitAmount = int.Parse(reminderSettings.OptVal("skipReminderLimitAmount"));
            var reminderFeeAmount = int.Parse(reminderSettings.OptVal("reminderFeeAmount"));

            settings.SkipReminderLimitAmount = skipReminderLimitAmount;
            settings.ReminderFeeAmount = reminderFeeAmount;

            return settings;
        }

        private static NotificationProcessSettings ConvertMortgageLoanNotificationSettingsModelToNotificationProcessSettings(MortgageLoanNotificationSettingsModel m)
        {
            return new NotificationProcessSettings
            {
                NotificationNotificationDay = m.NotificationNotificationDay,
                NotificationDueDay = m.NotificationDueDay,

                //Payment free months not implemented
                PaymentFreeMonthMinNrOfPaidNotifications = 10000, //Basically to turn this off
                PaymentFreeMonthExcludeNotificationFee = false,
                PaymentFreeMonthMaxNrPerYear = 0,

                ReminderFeeAmount = m.ReminderFeeAmount,
                MaxNrOfReminders = m.MaxNrOfReminders,
                NrOfFreeInitialReminders = m.NrOfFreeInitialReminders,
                TerminationLetterDueDay = m.TerminationLetterDueDay,
                ReminderMinDaysBetween = 14,
                FirstReminderDaysBefore = 14,
                AllowMissingCustomerAddress = true,

                //Not in use
                SkipReminderLimitAmount = 0,
                NotificationOverDueGraceDays = 0
            };
        }

        public void Dispose()
        {
            CachedSettingsService.RemoveSettingsChangedCallback(settingChangedCallbackId);
        }
    }
}