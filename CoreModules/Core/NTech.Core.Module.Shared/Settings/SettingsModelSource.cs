using NTech.Banking.BankAccounts;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Settings;
using NTech.Core.Module.Shared.Settings.KycUpdateFrequency;
using System;
using System.Collections.Generic;
using System.Linq;
using static NTech.Core.Customer.Shared.Settings.SettingsModel;

namespace NTech.Core.Customer.Shared.Settings
{
    public class SettingsModelSource
    {
        private readonly SettingsModel settings;
        private readonly SettingsValidationModel settingsValidation; 

        public SettingsModelSource(IClientConfigurationCore clientConfig)
        {
            var m = new SettingsModel
            {
                Settings = new List<SettingsModel.Setting>(),
                UiGroupDisplayNames = new Dictionary<string, string>()
            };
            var validationModel = new SettingsValidationModel();
            AddSettings(m, clientConfig, validationModel);
            SetupUiGroups(m);
            settings = m;
            settingsValidation = validationModel;
        }

        private static void AddSettings(SettingsModel m, IClientConfigurationCore clientConfig, SettingsValidationModel validationModel)
        {
            ////////////////////////////////////////
            ///// Component settings ///////////////
            ////////////////////////////////////////
            AddCreditHandlerLimitComponentSetting(m);
            AddBookKeepingRuleComponentSetting(m);
            AddUnsecuredLoanStandardPolicyFilterComponentSetting(m);
            AddKycQuestionTemplatesComponentSetting(m);

            ///////////////////////////////////////
            ///// Form settings ///////////////////
            ///////////////////////////////////////
            AddMortgageLoanWebAppSettings(m);
            AddUnsecuredLoanWebAppSettings(m, validationModel);
            AddEuEesTextAreaSettings(m);
            AddCreditRejectionSecureMessageTemplates(m, clientConfig);
            AddCustomerSecureMessageNotificationTemplate(m, clientConfig);
            AddEnableDisableSecureMessageToggles(m);
            AddCreditCreatedSecureMessageTemplates(m, clientConfig);
            AddDocumentClientDataForm(m);
            AddClientIncomingSecureMessageNotificationForm(m, clientConfig);
            AddLtlStressInterestForm(m, clientConfig);
            AddReminderSettings(m);
            AddMlStandardApplicationWorkflowEmailToggles(m, clientConfig);
            AddUlStandardApplicationWorkflowEmailToggles(m, clientConfig);
            AddKycQuestionUpdateRequiredSecureMessageTemplates(m, clientConfig);
            AddMortgageBindingExpirationReminderTemplate(m, clientConfig);
            AddAltPaymentPlanSecureMessageTemplates(m, clientConfig);
            AddMlStandardChangeTermsEmailForm(m, clientConfig);
            AddPaymentPlacementForm(m, clientConfig);
            AddLoanSettlementMessageTemplate(m);

            ////////////////////////////////////////
            /// Html template setting pages ////////
            /// ////////////////////////////////////
            AddGeneralTermsHtmlTemplatePage(m);

            ///////////////////////////////////////////
            /// Application automation settings //////
            /////////////////////////////////////////
            AddApplicationAutomationPage(m);

            /////////////////////////////////////
            // Bank acount settings /////////////
            /////////////////////////////////////
            AddIncomingPaymentAccountSettings(m, clientConfig);
            AddOutgoingPaymentAccountSettings(m, clientConfig);

            /////////////////////////////////////
            /// Kyc update frequency ///////////
            ///////////////////////////////////
            AddKycUpdateFrequencySettings(m, clientConfig);

            ////////////////////////////////
            ////Loan owner management ///
            //////////////////////////////
            AddMortgageLoanOwnerManagement(m, clientConfig);

            ///////////////////////////////////
            /// Petrus scoring settings //////
            /////////////////////////////////
            AddPetrusScoringSettings(m);

            /////////////////////////
            /// Psd2 settings //////
            ///////////////////////
            AddPsd2Settings(m);
        }

        public SettingsModel GetSettingsModel() => settings;

        public List<string> ValidateOnSave(string settingName, Dictionary<string, string> newValues) => settingsValidation.Validate(settingName, newValues);

        private static SettingsModelSource sharedSettingsSource;
        public static SettingsModelSource GetSharedSettingsModelSource(IClientConfigurationCore clientConfiguration)
        {
            if (sharedSettingsSource == null)
            {
                sharedSettingsSource = new SettingsModelSource(clientConfiguration);
            }
            return sharedSettingsSource;
        }

        /// <summary>
        /// Settings are added to groups using SettingsModel.IsPartOfUiGroup
        /// </summary>
        private static void SetupUiGroups(SettingsModel model)
        {
            model.SetUiGroupDisplayName("messageTemplateSettings", "E-mail and secure message templates");
            model.SetUiGroupDisplayName("paymentAccounts", "Payment accounts");

            var allSettingCodes = model.Settings.Select(x => x.Code).ToHashSetShared();
            var allUiGroupNames = model.Settings.Where(x => x.UiGroupName != null).Select(x => x.UiGroupName).ToHashSetShared();
            var overlappingNames = allSettingCodes.Intersect(allUiGroupNames).ToList();
            if (overlappingNames.Count > 0)
            {
                throw new Exception("There are ui group names that overlap with setting names which is not allowed");
            }
        }

        private static void AddCreditHandlerLimitComponentSetting(SettingsModel model)
        {
            model
                .AddComponentSetting("creditHandlerLimits", "Limit handler", "CreditHandlerLimitsComponent")
                .RequireFeaturesAny("ntech.feature.unsecuredloans", "ntech.feature.companyloans")
                .RequireFeaturesAll("ntech.feature.precredit")
                .RequireGroupsAny("High");
        }

        private static void AddBookKeepingRuleComponentSetting(SettingsModel model)
        {
            model
                .AddComponentSetting("editBookKeepingRules", "Bookkeeping", "BookkeepingRulesEditComponent")
                .RequireGroupsAny("High");
        }

        private static void AddUnsecuredLoanStandardPolicyFilterComponentSetting(SettingsModel model)
        {
            model
                .AddComponentSetting("loanPolicyFilters", "Policy filters", "LoanPolicyFiltersComponent")
                .RequireFeaturesAny("ntech.feature.unsecuredloans.standard", "ntech.feature.mortgageloans.standard")
                .RequireFeaturesAll("ntech.feature.precredit")
                .RequireGroupsAny("High");
        }

        private static void AddKycQuestionTemplatesComponentSetting(SettingsModel model)
        {
            model
                .AddComponentSetting("kycQuestionTemplates", "KYC Questions", "KycQuestionTemplatesComponent")
                .RequireFeaturesAll("feature.customerpages.kyc")
                .RequireGroupsAny("High");
        }

        private static void AddMortgageLoanWebAppSettings(SettingsModel model)
        {
            model
            .AddFormSetting("mortgageLoanExternalApplication", "Mortgage loan calculator")
            .RequireFeaturesAny("ntech.feature.mortgageloans.standard")
            .RequireFeaturesAll("ntech.feature.precredit")
            .RequireGroupsAny("High")
            .FormData
                .AddInterestField("exampleInterestRatePercent", "Example interest rate", x => x.ClientConfigDefaultValue("MortgageLoanProductSettings.WebApplication.ExampleInterestRatePercent"))
                .AddEnumField("isPurchaseTabActive", "Show purchase tab", x => x.ClientConfigDefaultValue("MortgageLoanProductSettings.WebApplication.IsPurchaseTabActive"),
                    Tuple.Create("true", "Yes"), Tuple.Create("false", "No"))
                .AddEnumField("isMoveTabActive", "Show move tab", x => x.ClientConfigDefaultValue("MortgageLoanProductSettings.WebApplication.IsMoveTabActive"),
                    Tuple.Create("true", "Yes"), Tuple.Create("false", "No"))
                .AddEnumField("isAdditionalLoanTabActive", "Show additional loan tab", x => x.ClientConfigDefaultValue("MortgageLoanProductSettings.WebApplication.IsAdditionalLoanTabActive"),
                    Tuple.Create("true", "Yes"), Tuple.Create("false", "No"))
                .AddIntegerField("maxLoanToValuePercent", "Max LTV%", x => x.ClientConfigDefaultValue("MortgageLoanProductSettings.WebApplication.MaxLoanToValuePercent"))
                .AddIntegerField("maxCurrentMortgageLoanAmount", "Max current mortgage loan amount", x => x.ClientConfigDefaultValue("MortgageLoanProductSettings.WebApplication.MaxCurrentMortgageLoanAmount"))
                .AddIntegerField("minCurrentMortgageLoanAmount", "Min current mortgage loan amount", x => x.ClientConfigDefaultValue("MortgageLoanProductSettings.WebApplication.MinCurrentMortgageLoanAmount"))
                .AddIntegerField("maxEstimatedValue", "Max estimated value", x => x.ClientConfigDefaultValue("MortgageLoanProductSettings.WebApplication.MaxEstimatedValue"))
                .AddIntegerField("minEstimatedValue", "Min estimated value", x => x.ClientConfigDefaultValue("MortgageLoanProductSettings.WebApplication.MinEstimatedValue"))
                .AddIntegerField("maxCashAmount", "Max cash amount", x => x.ClientConfigDefaultValue("MortgageLoanProductSettings.WebApplication.MaxCashAmount"))
                .AddIntegerField("minCashAmount", "Min cash amount", x => x.ClientConfigDefaultValue("MortgageLoanProductSettings.WebApplication.MinCashAmount"))
                .AddIntegerField("maxAdditionalLoanAmount", "Max additional loan amount", x => x.ClientConfigDefaultValue("MortgageLoanProductSettings.WebApplication.MaxAdditionalLoanAmount"))
                .AddIntegerField("minAdditionalLoanAmount", "Min additional loan amount", x => x.ClientConfigDefaultValue("MortgageLoanProductSettings.WebApplication.MinAdditionalLoanAmount"))
                .AddUrlField("personalDataPolicyUrl", "Personal data policy url", x => x.ClientConfigDefaultValue("MortgageLoanProductSettings.WebApplication.PersonalDataPolicyUrl"));
        }

        private static void AddUnsecuredLoanWebAppSettings(SettingsModel model, SettingsValidationModel settingsValidation)
        {
            const string settingName = "unsecuredLoanExternalApplication";
            
            var form = model
                .AddFormSetting(settingName, "Customer application")
                .RequireFeaturesAll("ntech.feature.unsecuredloans.standard", "ntech.feature.precredit", "ntech.feature.unsecuredloans.webapplication")
                .RequireGroupsAny("High")
                .FormData;

            form
                .AddInterestField("exampleInterestRatePercent", "Example interest rate", x => x.ClientConfigDefaultValue("UlStandardWebApplication.ExampleMarginInterestRatePercent"))
                .AddIntegerField("exampleNotificationFee", "Example notification fee", x => x.ClientConfigDefaultValue("UlStandardWebApplication.ExampleNotificationFee"))
                .AddIntegerField("exampleInitialFeeOnFirstNotificationAmount", "Example initial fee on first notification", x => x.ClientConfigDefaultValue("UlStandardWebApplication.ExampleInitialFeeOnFirstNotificationAmount"))

                .AddIntegerField("exampleLoanAmountMax", "Max loan amount", x => x.StaticDefaultValue(0))
                .AddIntegerField("exampleLoanAmountMin", "Min loan amount", x => x.StaticDefaultValue(0))
                .AddIntegerField("exampleLoanAmountStep", "Loan amount step", x => x.StaticDefaultValue(0))
                .AddIntegerField("exampleRepaymentMonthsMax", "Repayment time (months) - max", x => x.StaticDefaultValue(0))
                .AddIntegerField("exampleRepaymentMonthsMin", "Repayment time (months) - min", x => x.StaticDefaultValue(0))
                .AddIsEnabledField(false, overrideDisplayName: "One payment only enabled", overrideName: "exampleRepaymentDaysIsEnabled")
                .AddIntegerField("exampleRepaymentDaysMax", "Repayment time (days) - max", x => x.StaticDefaultValue(0))
                .AddIntegerField("exampleRepaymentDaysMin", "Repayment time (days) - min", x => x.StaticDefaultValue(0))

                .AddUrlField("personalDataPolicyUrl", "Personal data policy url", x => x.ClientConfigDefaultValue("UlStandardWebApplication.PersonalDataPolicyUrl"));

            settingsValidation.SetValidationRules(settingName, x =>
            {
                void CheckMaxMin(string maxName, string minName)
                {
                    var max = x.GetInt(maxName);
                    var min = x.GetInt(minName);

                    x.AddErrorIf(max < min, $"{form.GetDisplayName(maxName)} < {form.GetDisplayName(minName)}");
                    x.AddErrorIf((max == 0) != (min == 0), $"{form.GetDisplayName(maxName)}, {form.GetDisplayName(minName)}: If either is zero both must be");
                }
                CheckMaxMin("exampleLoanAmountMax", "exampleLoanAmountMin");
                var zeroCount = new[] { x.GetInt("exampleLoanAmountStep"), x.GetInt("exampleLoanAmountMax"), x.GetInt("exampleLoanAmountMin") }.Where(y => y == 0).Count();
                x.AddErrorIf(
                    zeroCount > 0 && zeroCount < 3, 
                    $"{form.GetDisplayName("exampleLoanAmountStep")}, {form.GetDisplayName("exampleLoanAmountMax")}, {form.GetDisplayName("exampleLoanAmountMin")}: All or none must be zero");

                CheckMaxMin("exampleRepaymentMonthsMax", "exampleRepaymentMonthsMin");
                if(x.GetBool("exampleRepaymentDaysIsEnabled"))
                {
                    CheckMaxMin("exampleRepaymentDaysMax", "exampleRepaymentDaysMin");
                    x.AddErrorIf(x.GetInt("exampleRepaymentDaysMax") > 30, $"{form.GetDisplayName("exampleRepaymentDaysMax")} > 30");
                    x.AddErrorIf(x.GetInt("exampleRepaymentDaysMin") < 10, $"{form.GetDisplayName("exampleRepaymentDaysMin")} < 10");
                }
            });
        }

        private static void AddEuEesTextAreaSettings(SettingsModel model)
        {
            var defaultEuAndEesCountryCodes2022 = new List<string> { "AX", "BE", "BG", "CY", "DK", "EE", "FI", "FR", "GR", "IE", "IS", "IT", "HR", "LV", "LI", "LT", "LU", "MT", "NL", "NO", "AT", "PL", "PT", "RO", "SM", "SK", "SI", "ES", "SJ", "SE", "CZ", "DE", "HU", "VA" };
            var defaultEesOnlyCountryCodes2022 = new List<string> { "AX", "IS", "LI", "NO", "SM", "SJ", "VA" };

            string CommaSeparated(List<string> list) => string.Join(",", list);

            model
            .AddFormSetting("euEesCountryCodes", "EU/EES countries")
            .RequireGroupsAny("High")
            .FormData
                .AddTextAreaField("euAndEesCountryCodes", "EU/EES ISO two letter country codes",
                    6, x => x.StaticDefaultValue(CommaSeparated(defaultEuAndEesCountryCodes2022)))
                .AddTextAreaField("eesOnlyCountryCodes", "EES only ISO two letter country codes", 6, x => x.StaticDefaultValue(CommaSeparated(defaultEesOnlyCountryCodes2022)));
        }

        private static void AddMlStandardApplicationWorkflowEmailToggles(SettingsModel model, IClientConfigurationCore clientConfig)
        {
            void AddSetting(string name, string displayName)
            {
                var isSweden = clientConfig.Country.BaseCountry == "SE";

                var genericSubjectTemplateInitialValue = isSweden
                    ? "Uppdatering angående ansökan"
                    : "Update regarding application";

                var genericBodyTemplateInitialValue = isSweden
                    ? "Det finns en uppdatering angående din ansökan hos  {{clientDisplayName}} som kan ses på [mina sidor]({{customerPagesLink}})."
                    : "There is an update regarding your application with {{clientDisplayName}} that can be read on [customer pages]({{customerPagesLink}}).";

                model
                    .AddFormSetting(name, $"Email template: {displayName}")
                    .RequireGroupsAny("High")
                    .IsPartOfUiGroup("messageTemplateSettings")
                    .RequireFeaturesAny("ntech.feature.mortgageloans.standard")
                    .RequireFeaturesAll("ntech.feature.precredit")
                    .FormData
                        .AddIsEnabledField(true)
                        .AddTextAreaField("genericSubjectTemplate", "Subject", 3, x => x.StaticDefaultValue(genericSubjectTemplateInitialValue))
                        .AddTextAreaField("genericBodyTemplate", "Body", 3, x => x.StaticDefaultValue(genericBodyTemplateInitialValue));
            }

            AddSetting("initialCreditCheckApproveEmailTemplates", "Credit check approved - initial");
            AddSetting("finalCreditCheckApproveEmailTemplates", "Credit check approved - final");
        }

        private static void AddUlStandardApplicationWorkflowEmailToggles(SettingsModel model, IClientConfigurationCore clientConfig)
        {
            void AddSetting(string name, string displayName)
            {
                var isSweden = clientConfig.Country.BaseCountry == "SE";

                var genericSubjectTemplateInitialValue = isSweden
                    ? "Uppdatering angående ansökan"
                    : "Update regarding application";

                var genericBodyTemplateInitialValue = isSweden
                    ? "Det finns en uppdatering angående din ansökan hos  {{clientDisplayName}} som kan ses på [mina sidor]({{customerPagesLink}})."
                    : "There is an update regarding your application with {{clientDisplayName}} that can be read on [customer pages]({{customerPagesLink}}).";

                model
                    .AddFormSetting(name, $"Email template: {displayName}")
                    .RequireGroupsAny("High")
                    .IsPartOfUiGroup("messageTemplateSettings")
                    .RequireFeaturesAny("ntech.feature.unsecuredloans.standard")
                    .FormData
                        .AddIsEnabledField(true)
                        .AddTextAreaField("genericSubjectTemplate", "subject", 3, x => x.StaticDefaultValue(genericSubjectTemplateInitialValue))
                        .AddTextAreaField("genericBodyTemplate", "Body", 3, x => x.StaticDefaultValue(genericBodyTemplateInitialValue));
            }

            AddSetting("creditCheckApproveEmailTemplates", "Credit check approved");
            AddSetting("agreementReadyForSigningEmailTemplates", "Agreement ready for signing");
        }

        private static void AddCreditRejectionSecureMessageTemplates(SettingsModel model, IClientConfigurationCore clientConfig)
        {
            var isSweden = clientConfig.Country.BaseCountry == "SE";

            var defaultTemplateInitialValue = isSweden
                ? "Vi kan tyvärr inte bevilja det sökta lånet. Beslutet baserar sig på en helhetsbedömning utifrån data från ansökan."
                : "The requested loan could not be granted. The decision is based on an overall assessment of the application.";

            model
                .AddFormSetting("creditRejectionSecureMessageTemplates", "Secure message: On credit check rejection")
                .RequireGroupsAny("High")
                .RequireFeaturesAny("ntech.feature.unsecuredloans.standard", "ntech.feature.mortgageloans.standard")
                .RequireFeaturesAll("ntech.feature.precredit")
                .IsPartOfUiGroup("messageTemplateSettings")
                .FormData
                    .AddIsEnabledField(true)
                    .AddTextAreaField("generalTemplate", "General rejection", 3, x => x.StaticDefaultValue(defaultTemplateInitialValue))
                    .AddTextAreaField("paymentRemarkTemplate", "Payment remark rejection", 3, x => x.StaticDefaultValue(defaultTemplateInitialValue));
        }

        private static void AddCustomerSecureMessageNotificationTemplate(SettingsModel m, IClientConfigurationCore clientConfig)
        {
            var defaultSubjectTemplateInitialValue = clientConfig.Country.BaseCountry == "SE"
                ? "Du har ett nytt meddelande från {{clientDisplayName}}"
                : "You have a new message from {{clientDisplayName}}";

            var defaultBodyTemplateInitialValue = clientConfig.Country.BaseCountry == "SE"
                ? "Du har ett nytt meddelande från {{clientDisplayName}} som kan läsas på [mina sidor]({{customerPagesLink}})."
                : "You have a new message from {{clientDisplayName}} that can be read on [customer pages]({{customerPagesLink}}).";

            m
                .AddFormSetting("customerSecureMessageEmailNotification", "Email template: Secure message notification")
                .RequireGroupsAny("High")
                .RequireFeaturesAny("ntech.feature.unsecuredloans.standard", "ntech.feature.mortgageloans.standard")
                .RequireFeaturesAll("ntech.feature.securemessages")
                .IsPartOfUiGroup("messageTemplateSettings")
                .FormData
                    .AddTextAreaField("messageSubjectTemplate", "Message body", 3, x => x.StaticDefaultValue(defaultSubjectTemplateInitialValue))
                    .AddTextAreaField("messageBodyTemplate", "Email body", 3, x => x.StaticDefaultValue(defaultBodyTemplateInitialValue));
        }

        private static void AddEnableDisableSecureMessageToggles(SettingsModel m)
        {
            m
                .AddFormSetting("enableDisableSecureMessages", "Enable/disable secure messages")
                .RequireGroupsAny("High")
                .RequireFeaturesAny("ntech.feature.unsecuredloans.standard", "ntech.feature.mortgageloans.standard")
                .RequireFeaturesAll("ntech.feature.precredit", "ntech.feature.securemessages")
                .IsPartOfUiGroup("messageTemplateSettings")
                .FormData
                    .AddEnumField("isInactiveMessagingAllowed", "Allow customer to send messages on inactive applications:", x => x.StaticDefaultValue("true"), Tuple.Create("true", "Yes"), Tuple.Create("false", "No"));
        }
        private static void AddCreditCreatedSecureMessageTemplates(SettingsModel model, IClientConfigurationCore clientConfig)
        {
            var isSweden = clientConfig.Country.BaseCountry == "SE";

            var defaultTemplateInitialValue = isSweden
                ? "Ditt lån är nu upplagt och du kan följa det här på mina sidor. Utbetalningen bör vara dig tillhanda inom några dagar."
                : "Your loan has been created and you can track it here on my pages. The payment should be in your bank account within a few days.";

            model
                .AddFormSetting("creditCreatedSecureMessageTemplates", "Secure message: On loan creation")
                .RequireGroupsAny("High")
                .IsPartOfUiGroup("messageTemplateSettings")
                .RequireFeaturesAny("ntech.feature.unsecuredloans.standard")
                .FormData
                    .AddIsEnabledField(true)
                    .AddTextAreaField("templateText", "Template text", 3, x => x.StaticDefaultValue(defaultTemplateInitialValue));
        }

        private static void AddGeneralTermsHtmlTemplatePage(SettingsModel model)
        {
            var defaultValue = "";

            model
                .AddHtmlTemplateSetting("generalTermsHtmlTemplate", "General terms template", defaultValue)
                .RequireGroupsAny("High")
                .RequireFeaturesAny("ntech.feature.unsecuredloans.standard", "ntech.feature.mortgageloans.standard");
        }

        private static void AddApplicationAutomationPage(SettingsModel model)
        {
            model
                .AddFormSetting("applicationAutomation", "Application automation")
                .RequireGroupsAny("High")
                .RequireFeaturesAny("ntech.feature.unsecuredloans.standard")
                .FormData
                    .AddEnumField("creditCheckRecommendedReject", "Credit check recommended reject: Reject application", x => x.StaticDefaultValue("false"), Tuple.Create("true", "Enabled"), Tuple.Create("false", "Disabled"))
                    .AddEnumField("customerAcceptsOfferApproveStep", "Customer accepts offer: Approve step", x => x.StaticDefaultValue("false"), Tuple.Create("true", "Enabled"), Tuple.Create("false", "Disabled"))
                    .AddEnumField("customerRejectsOfferCancelApplication", "Customer rejects offer: Cancel application", x => x.StaticDefaultValue("false"), Tuple.Create("true", "Enabled"), Tuple.Create("false", "Disabled"))
                    .AddEnumField("kycChecksOkApproveStep", "KYC all checks ok: Approve step", x => x.StaticDefaultValue("false"), Tuple.Create("true", "Enabled"), Tuple.Create("false", "Disabled"))
                    .AddEnumField("fraudChecksOkApproveStep", "Fraud all checks ok: Approve step", x => x.StaticDefaultValue("false"), Tuple.Create("true", "Enabled"), Tuple.Create("false", "Disabled"))
                    .AddEnumField("agreementCreatedSendOutForSigning", "Agreement created: Send out for signing", x => x.StaticDefaultValue("false"), Tuple.Create("true", "Enabled"), Tuple.Create("false", "Disabled"))
                    .AddEnumField("agreementSignedApproveStep", "Agreement signed: Approve step", x => x.StaticDefaultValue("false"), Tuple.Create("true", "Enabled"), Tuple.Create("false", "Disabled"));
        }

        private static void AddPetrusScoringSettings(SettingsModel model)
        {
            model
                .AddFormSetting("petrusScoringSettings", "Petrus settings")
                .RequireGroupsAny("High")
                .RequireFeaturesAny("ntech.feature.ullegacy")
                .FormData
                    .AddTextField("url", "Url", x => x.StaticDefaultValue("none"))
                    .AddTextField("username", "Username", x => x.StaticDefaultValue("none"))
                    .AddHiddenTextField("password", "Password", x => x.StaticDefaultValue("none"))
                    .AddIsEnabledField(false, overrideName: "isRequestLoggingEnabled", overrideDisplayName: "Enable request logging");
        }

        private static void AddPsd2Settings(SettingsModel model)
        {
            model
                .AddFormSetting("psd2Settings", "Psd2 settings")
                .RequireGroupsAny("High")
                .RequireFeaturesAny("ntech.feature.ullegacy")
                .FormData
                     .AddIsEnabledField(true)
                     .AddIsEnabledField(false, overrideName: "isForcedBankAccountDataSharingEnabled", overrideDisplayName: "Is forced bank account data sharing enabled");
        }

        private static void AddDocumentClientDataForm(SettingsModel model)
        {
            var form = model
                .AddFormSetting("documentClientData", "Document Clientdata")
                .RequireFeaturesAny("ntech.feature.unsecuredloans.standard", "ntech.feature.mortgageloans.standard", "ntech.feature.unsecuredloans")
                .RequireGroupsAny("High")
                .FormData;

            void AddField(string name, string displayName)
            {
                var path = $"DocumentClientData.{name.Substring(0, 1).ToUpper()}{name.Substring(1)}";
                form.AddTextField(name, displayName, x => x.ClientConfigDefaultValue(path));
            }

            AddField("orgnr", "Orgnr");
            AddField("name", "Name");
            AddField("streetAddress", "Street address");
            AddField("zipCode", "Zip code");
            AddField("postalArea", "Postal area");
            AddField("footerAddress", "Footer address");
            AddField("contactText", "Contact text");
            AddField("email", "Email");
            AddField("website", "Website");
        }

        private static void AddClientIncomingSecureMessageNotificationForm(SettingsModel model, IClientConfigurationCore clientConfig)
        {
            var isSweden = clientConfig.Country.BaseCountry == "SE";

            var defaultSubjectTemplate = isSweden
                ? "Nytt säkert meddelande {{channelId}}"
                : "New secure message {{channelId}}";

            var defaultBodyTemplate = isSweden
                ? "Det finns ett nytt säkert meddelande: [{{channelId}}]({{notificationBackOfficeUrl}})"
                : "There is a new secure message: [{{channelId}}]({{notificationBackOfficeUrl}})";

            var form = model
                .AddFormSetting("clientIncomingSecureMessageNotifications", "Secure message: Internal notification")
                .RequireFeaturesAny("ntech.feature.unsecuredloans.standard", "ntech.feature.mortgageloans.standard")
                .RequireFeaturesAll("ntech.feature.securemessages")
                .RequireGroupsAny("High")
                .IsPartOfUiGroup("messageTemplateSettings")
                .FormData;

            form.AddIsEnabledField(false);
            form.AddTextField("clientGroupEmail", "Notification email", x => x.StaticDefaultValue("none"));
            form.AddTextField("notificationTemplateSubjectText", "Notification subject", x => x.StaticDefaultValue(defaultSubjectTemplate));
            form.AddTextAreaField("notificationTemplateBodyText", "Notification body", 3, x => x.StaticDefaultValue(defaultBodyTemplate));
        }

        private static void AddLtlStressInterestForm(SettingsModel model, IClientConfigurationCore clientConfig)
        {
            var isMortgageLoan = clientConfig.IsFeatureEnabled("ntech.feature.mortgageloans.standard");

            model
                .AddFormSetting("ltlStressInterest", "LTL Stress interest")
                .RequireGroupsAny("High")
                .RequireFeaturesAny("ntech.feature.unsecuredloans.standard", "ntech.feature.mortgageloans.standard")
                .RequireFeaturesAll("ntech.feature.precredit")
                .FormData
                    .AddInterestField("stressInterestRatePercent", "Stress interest (%)", x => x.StaticDefaultValue(isMortgageLoan ? 4.5m : 20m));
        }

        private static void AddReminderSettings(SettingsModel model)
        {
            model
                .AddFormSetting("reminderFeeSettings", "Reminders")
                .RequireGroupsAny("High")
                .RequireFeaturesAny("ntech.feature.unsecuredloans", "ntech.feature.unsecuredloans.standard", "ntech.feature.mortgageloans.standard")
                .FormData
                    .AddIsEnabledField(false, overrideDisplayName: "Use overrides")
                    .AddIntegerField("skipReminderLimitAmount", "Override limit amount", x => x.StaticDefaultValue(0))
                    .AddIntegerField("reminderFeeAmount", "Override fee amount", x => x.StaticDefaultValue(0));
        }

        private static void AddOutgoingPaymentAccountSettings(SettingsModel model, IClientConfigurationCore clientConfig)
        {
            ISet<BankAccountNumberTypeCode> onlyTheseBankAccountNrTypes;
            if (clientConfig.Country.BaseCountry == "SE")
            {
                onlyTheseBankAccountNrTypes = new HashSet<BankAccountNumberTypeCode> { BankAccountNumberTypeCode.BankAccountSe };
            }
            else if (clientConfig.Country.BaseCountry == "FI")
            {
                onlyTheseBankAccountNrTypes = new HashSet<BankAccountNumberTypeCode> { BankAccountNumberTypeCode.IBANFi };
            }
            else
            {
                //Dont show this setting at all for countries where we havent figured out which account types are appropriate
                return;
            }
            model
                .AddBankAccountSetting("outgoingPaymentSourceBankAccount", "Outgoing payment account", false, onlyTheseBankAccountNrTypes: onlyTheseBankAccountNrTypes)
                .RequireFeaturesAny("High")
                .RequireFeaturesAny("ntech.feature.unsecuredloans.standard", "ntech.feature.mortgageloans.standard")
                .IsPartOfUiGroup("paymentAccounts");
        }

        private static void AddIncomingPaymentAccountSettings(SettingsModel model, IClientConfigurationCore clientConfig)
        {
            ISet<BankAccountNumberTypeCode> onlyTheseBankAccountNrTypes;
            if (clientConfig.Country.BaseCountry == "SE")
            {
                onlyTheseBankAccountNrTypes = new HashSet<BankAccountNumberTypeCode> { BankAccountNumberTypeCode.BankGiroSe };
            }
            else if (clientConfig.Country.BaseCountry == "FI")
            {
                onlyTheseBankAccountNrTypes = new HashSet<BankAccountNumberTypeCode> { BankAccountNumberTypeCode.IBANFi };
            }
            else
            {
                //Dont show this setting at all for countries where we havent figured out which account types are appropriate
                return;
            }
            model
                .AddBankAccountSetting("incomingPaymentBankAccount", "Incoming payment account", false, onlyTheseBankAccountNrTypes: onlyTheseBankAccountNrTypes)
                .RequireFeaturesAny("High")
                .RequireFeaturesAny("ntech.feature.unsecuredloans.standard", "ntech.feature.mortgageloans.standard")
                .IsPartOfUiGroup("paymentAccounts");
        }

        private static void AddKycQuestionUpdateRequiredSecureMessageTemplates(SettingsModel model, IClientConfigurationCore clientConfig)
        {
            var templateTextInitialValue = "You have not updated your KYC information for a while. Please update your KYC information as soon as possible.";

            model
                .AddFormSetting("kycUpdateRequiredSecureMessage", "Secure message: KYC update notification")
                .IsPartOfUiGroup("messageTemplateSettings")
                .RequireGroupsAny("High")
                .RequireFeaturesAll("feature.customerpages.kyc")
                .FormData
                    .AddIsEnabledField(true)
                    .AddIntegerField("nrOfDaysBeforeUpdate", "Nr of days before update", x => x.StaticDefaultValue(0))
                    .AddIsEnabledField(false, overrideName: "additionalNotificationIsEnabled", overrideDisplayName: "Additional notifications enabled?")
                    .AddIntegerField("additionalNotificationFrequency", "Additional notification frequency", x => x.StaticDefaultValue(0))
                    .AddIsEnabledField(false, overrideName: "isOverdueLoginRedirectEnabled", overrideDisplayName: "Redirect to overdue questions on login?")
                    .AddTextAreaField("templateText", "Message text", 3, x => x.StaticDefaultValue(templateTextInitialValue));
        }

        private static void AddKycUpdateFrequencySettings(SettingsModel m, IClientConfigurationCore clientConfig)
        {
            var s = new Setting
            {
                Code = "kycUpdateFrequency",
                DisplayName = "KYC update frequency",
                Type = SettingTypeCode.KycUpdateFrequency.ToString(),
                Features = new FeaturesModel
                {
                    RequireAny = new HashSet<string>(),
                    RequireAll = new HashSet<string>()
                },
                Groups = new GroupsModel
                {
                    RequireAny = new HashSet<string>()
                },
                KycUpdateFrequencyData = new KycUpdateFrequencyDataModel
                {
                    DefaultMonthCount = 24
                }
            };
            m.Settings.Add(s);
            s
                .RequireGroupsAny("High")
                .RequireFeaturesAll("feature.customerpages.kyc");
        }

        private static void AddMortgageLoanOwnerManagement(SettingsModel m, IClientConfigurationCore clientConfig)
        {
            if (clientConfig.Country.BaseCountry == "SE")
            {
                m
                 .AddAddRemoveRowsSetting("loanOwnerManagement", "Loan owner management")
                 .RequireGroupsAny("High")
                 .RequireFeaturesAny("ntech.feature.mortgageloans.standard");
            }
            else
            {
                //Dont show this setting at all for other countries
                return;
            }
        }

        private static void AddMortgageBindingExpirationReminderTemplate(SettingsModel model, IClientConfigurationCore clientConfig)
        {
            if (clientConfig.Country.BaseCountry != "SE")
                return;

            const string defaultText = "Din bindingstid går snart ut. Kontakta oss om du vill binda om lånet. Om du inte vill binda om lånet så kommer ditt lån automatiskt gå över till 3 månaders bindingstid.";

            model
                .AddFormSetting("mlBindingExpirationSecureMessage", "Secure message: Binding period ends")
                .IsPartOfUiGroup("messageTemplateSettings")
                .RequireGroupsAny("High")
                .RequireFeaturesAny("ntech.feature.mortgageloans.standard")
                .FormData
                    .AddIsEnabledField(false)
                    .AddTextAreaField("templateText", "Message text", 3, x => x.StaticDefaultValue(defaultText));
        }

        private static void AddAltPaymentPlanSecureMessageTemplates(SettingsModel model, IClientConfigurationCore clientConfig)
        {
            var onCreatedDefaultTemplateInitialValue =
                @"An alternate payment plan on {{creditNr}} has been created with these payments.<br><br>
                {{#months}}
                {{dueDate}}: {{monthlyAmount}}<br>
                {{/months}}<br><br>
                For a total of {{totalAmountToPay}} no later than {{lastDueDate}}.<br><br>
                Payments should be made to:<br>
                Bank account: {{payToBankAccount}}<br>
                Reference: {{ocrReference}}";
            var onNotificationDefaultTemplateInitialValue =
                @"You have an upcoming payment on your payment plan on {{creditNr}}.<br><br>
                Payments should be made to:<br>
                Amount: {{remainingMonthlyAmount}}<br>
                Bank account: {{payToBankAccount}}<br>
                Reference: {{ocrReference}}<br>
                Latest payment date: {{dueDate}}";
            var onMissedPaymentDefaultTemplateInitialValue =
                @"Your payment plan payment due {{dueDate}} has not been received.<br>
                Additonal payment of at least {{minimumAmountToPay}} is required right away or the payment plan will be cancelled.<br><br>
                Payments should be made to:<br>
                Bank account: {{payToBankAccount}}<br>
                Reference: {{ocrReference}}<br>
                Latest payment date: {{dueDate}}";

            model
                .AddFormSetting("altPaymentPlanSecureMessageTemplates", "Secure message: Alternate payment plan")
                .IsPartOfUiGroup("messageTemplateSettings")
                .RequireGroupsAny("High")
                .RequireFeaturesAny("ntech.feature.paymentplan")
                .FormData
                    .AddIsEnabledField(true, overrideName: "onCreated", overrideDisplayName: "On created")
                    .AddTextAreaField("onCreatedTemplateText", "Message text", 5, x => x.StaticDefaultValue(onCreatedDefaultTemplateInitialValue))
                    .AddIsEnabledField(true, overrideName: "onNotification", overrideDisplayName: "On notification")
                    .AddTextAreaField("onNotificationTemplateText", "Message text", 5, x => x.StaticDefaultValue(onNotificationDefaultTemplateInitialValue))
                    .AddIsEnabledField(true, overrideName: "onMissedPayment", overrideDisplayName: "On missed payment")
                    .AddIntegerField("nrOfDaysOnMissedPayment", "Missed payment: min nr of days", x => x.StaticDefaultValue(5))
                    .AddTextAreaField("onMissedPaymentTemplateText", "Message text", 5, x => x.StaticDefaultValue(onMissedPaymentDefaultTemplateInitialValue));
        }

        private static void AddMlStandardChangeTermsEmailForm(SettingsModel model, IClientConfigurationCore clientConfig)
        {
            var isSweden = clientConfig.Country.BaseCountry == "SE";

            var defaultSubjectTemplate = isSweden
                ? "Signering av villkorsändring"
                : "Signature for term change";

            
            var defaultBodyTemplate = isSweden
                ? "Klicka på [länken]({{link}}) för att granska och signera de nya villkoren. När alla parter signerat kommer de nya villkoren aktiveras på det avtalade datumet."
                : "Click the [Link]({{link}}) to review and sign the new terms. When the all parties has signed, the new terms will be activated on agreed date.";

            var form = model
                .AddFormSetting("mlStandardChangeTermsEmailTemplates", "Change terms: Signature email")
                .RequireFeaturesAny("ntech.feature.mortgageloans.standard")
                .RequireGroupsAny("High")
                .IsPartOfUiGroup("messageTemplateSettings")
                .FormData;

            form.AddIsEnabledField(true);
            form.AddTextField("templateSubjectText", "Subject", x => x.StaticDefaultValue(defaultSubjectTemplate));
            form.AddTextAreaField("templateBodyText", "Body", 3, x => x.StaticDefaultValue(defaultBodyTemplate));
        }

        private static void AddPaymentPlacementForm(SettingsModel model, IClientConfigurationCore clientConfig)
        {
            int defaultMaxUiNotNotifiedCapitalWriteOffAmount = 0;
            if (clientConfig.Country.BaseCountry == "SE")
                defaultMaxUiNotNotifiedCapitalWriteOffAmount = 100;
            else if(clientConfig.Country.BaseCountry == "FI")
                defaultMaxUiNotNotifiedCapitalWriteOffAmount = 10;

            model
                .AddFormSetting("paymentPlacement", "Payment placement")
                .RequireGroupsAny("High")
                .FormData
                .AddIntegerField("maxUiNotNotifiedCapitalWriteOffAmount", "Max placement ui not notified capital writeoff amount", x => x.StaticDefaultValue(defaultMaxUiNotNotifiedCapitalWriteOffAmount.ToString()));
        }

        private static void AddLoanSettlementMessageTemplate(SettingsModel model)
        {
            const string defaultText = @"Hei! 
Brocc-lainasi {{creditNr}} on nyt kokonaan maksettu. Kiitos, että olit asiakkaanamme!
Ystävällisin terveisin,
Brocc Asiakaspalvelu

Hej! 
Ditt Brocc-lån {{creditNr}} är nu fullbetalt. Vi vill passa på att tacka för att du har varit vår kund! 
Med vänlig hälsning,
Brocc Kundtjänst";

            model
                .AddFormSetting("loanSettledSecureMessage", "Secure message: On loan settlement")
                .IsPartOfUiGroup("messageTemplateSettings")
                .RequireGroupsAny("High")
                .RequireFeaturesAny("ntech.feature.ullegacy")
                .FormData
                    .AddIsEnabledField(false)
                    .AddTextAreaField("templateText", "Message text", 3, x => x.StaticDefaultValue(defaultText));
        }
    }
}