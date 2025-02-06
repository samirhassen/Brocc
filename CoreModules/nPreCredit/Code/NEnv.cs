using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using nPreCredit.Code;
using nPreCredit.Code.Balanzia;
using nPreCredit.Code.MortgageLoans;
using nPreCredit.Code.Services;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.CivicRegNumbers;
using NTech.Banking.OrganisationNumbers;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace nPreCredit
{
    public static class NEnv
    {
        private static Lazy<LegacyPreCreditEnvSettings> envSettings = new Lazy<LegacyPreCreditEnvSettings>(() => new LegacyPreCreditEnvSettings());

        public static IPreCreditEnvSettings EnvSettings => envSettings.Value;

        public static string CurrentServiceName => envSettings.Value.CurrentServiceName;

        public static bool IsBundlingEnabled
        {
            get
            {
                var d = new Dictionary<string, string>();
                var s = Opt("ntech.isbundlingenabled") ?? "true";
                return s.Trim().ToLower() == "true";
            }
        }

        public static bool ForceManualControlOnInitialScoring => OptBool("ntech.precredit.scoring.forcemanualcontroloninitial");

        public static string NTechCdnUrl => Opt("ntech.cdn.rooturl");

        public static DirectoryInfo LogFolder => envSettings.Value.LogFolder;

        public static ClientConfiguration ClientCfg
        {
            get
            {
                return envSettings.Value.ClientCfg;
            }
        }
        public static IClientConfigurationCore ClientCfgCore =>
            NTechCache.WithCache("nPreCredit.ClientCfgCore", TimeSpan.FromMinutes(15), () => ClientConfigurationCoreFactory.CreateUsingNTechEnvironment(E));

        public static bool IsProduction => envSettings.Value.IsProduction;

        public static ScoringSetupModel ScoringSetup => envSettings.Value.ScoringSetup;

        public static XDocument CompanyLoanRejectionReasonsFile
        {
            get
            {
                return NTechCache.WithCache("b5s7cb07c-c0fe-4a22-9502-5107fabsd8a5d3", TimeSpan.FromMinutes(15), () =>
                {
                    var f = E.ClientResourceFile("ntech.companyloan.rejectionreasonsfile", "CompanyLoan-Scoring-RejectionReasons.xml", true);
                    return XDocuments.Load(f.FullName);
                });
            }
        }

        public static MortgageLoanScoringSetup MortgageLoanScoringSetup
        {
            get
            {
                return NTechCache.WithCache("edc6e4dc-13c3-4b60-b34e-84ddc408dce4", TimeSpan.FromMinutes(15), () =>
                {
                    var f = E.ClientResourceFile("ntech.precredit.mortgagescoringsetupfile", "MortgageLoan-Scoring.json", true);
                    return JsonConvert.DeserializeObject<MortgageLoanScoringSetup>(File.ReadAllText(f.FullName));
                });
            }
        }

        public static SignatureProviderCode? SignatureProvider => envSettings.Value.SignatureProvider;

        public static string CreditReportProviderName => envSettings.Value.CreditReportProviderName;
        public static string[] ListCreditReportProviders => envSettings.Value.ListCreditReportProviders;

        public static CivicRegNumberParser BaseCivicRegNumberParser
        {
            get
            {
                return NTechCache.WithCache("BaseCivicRegNumberParser", TimeSpan.FromMinutes(5), () => new CivicRegNumberParser(ClientCfg.Country.BaseCountry));
            }
        }

        public static OrganisationNumberParser BaseOrganisationNumberParser
        {
            get
            {
                return NTechCache.WithCache("BaseOrganisationNumberParser", TimeSpan.FromMinutes(5), () => new OrganisationNumberParser(ClientCfg.Country.BaseCountry));
            }
        }

        public static string DefaultScoringVersion => Opt("ntech.precredit.scoringversion") ?? ClientCfg.OptionalSetting("ntech.precredit.scoringversion");

        public static decimal? MortgageLoanClientMinimumAmortizationPercent
        {
            get
            {
                var v = Opt("ntech.mortgageloan.minimumamortizationpercent") ?? ClientCfg.OptionalSetting("ntech.mortgageloan.minimumamortizationpercent");
                return v == null ? new decimal?() : decimal.Parse(v, CultureInfo.InvariantCulture);
            }
        }

        public static int? MortgageLoanDefaultAmortizationFreeMonths
        {
            get
            {
                var v = Opt("ntech.mortgageloan.defaultamortizationfreemonths") ?? ClientCfg.OptionalSetting("ntech.mortgageloan.defaultamortizationfreemonths") ?? "60";
                return v == null ? new int?() : int.Parse(v, CultureInfo.InvariantCulture);
            }
        }

        private static Lazy<IBANToBICTranslator> iBANToBICTranslatorInstance = new Lazy<IBANToBICTranslator>(() => new IBANToBICTranslator());

        public static IBANToBICTranslator IBANToBICTranslatorInstance => iBANToBICTranslatorInstance.Value;

        public static DirectoryInfo AffiliatesFolder => envSettings.Value.AffiliatesFolder;

        public static Code.Services.AdServiceIntegrationService.AdServicesSettings AdServicesSettings
        {
            get
            {
                return NTechCache.WithCache("edc6ase4dc-13c3-4b60-dsab34e-84ddc408dce4", TimeSpan.FromMinutes(15), () =>
                {
                    var f = E.StaticResourceFile("ntech.precredit.adservicessettingsfile", "adservices-settings.txt", false);
                    if (f == null || !f.Exists)
                        return new Code.Services.AdServiceIntegrationService.AdServicesSettings
                        {
                            IsEnabled = false
                        };

                    var s = NTechSimpleSettings.ParseSimpleSettingsFile(f.FullName);
                    var isEnabled = s.ReqBool("enabled");
                    var endpointUrl = isEnabled ? s.Req("endpointUrl") : s.Opt("endpointUrl");
                    return new Code.Services.AdServiceIntegrationService.AdServicesSettings
                    {
                        IsEnabled = isEnabled,
                        CampaignId = isEnabled ? s.Req("campaignId") : s.Opt("campaignId"),
                        EndpointUrl = endpointUrl == null ? null : new Uri(endpointUrl)
                    };
                });
            }
        }

        public static AffiliateModel GetAffiliateModel(string providerName, bool allowMissing = false) =>
            envSettings.Value.GetAffiliateModel(providerName, allowMissing: allowMissing);

        public static List<AffiliateModel> GetAffiliateModels() => envSettings.Value.GetAffiliateModels();

        public static DirectoryInfo AffiliateReportingSourceFolder => E.StaticResourceDirectory("ntech.precredit.affiliatereporting.sourcefolder", "AffiliateReporting", false);

        public static List<string> PluginSourceFolders
        {
            get
            {
                var v = Opt("ntech.precredit.plugins.sourcefolders");
                if (v != null)
                    return v.Split(';').Select(x => x).ToList();
                else
                    return null;
            }
        }

        public static bool IsAllowedToAutoFollowAcceptedCreditDecisions
        {
            get
            {
                var v = Opt("ntech.automation.creditcheck.followaccepted") ?? ClientCfg.OptionalSetting("ntech.automation.creditcheck.followaccepted");
                return v?.Trim()?.ToLowerInvariant() == "true";
            }
        }

        public static List<string> EnabledPluginNames
        {
            get
            {
                var v = Opt("ntech.precredit.plugins.enablednames");
                if (v != null)
                    return v.Split(';').ToList();
                else
                    return new List<string>();
            }
        }

        public static bool IsCreditCheckAutomationEnabled
        {
            get
            {
                var isCreditCheckAutomationEnabledOnClient = (ClientCfg.OptionalSetting("ntech.automation.creditcheck.enabled") ?? "false").ToLowerInvariant().Trim() == "true";
                var isCreditCheckAutomatinDisabledByAppsetting = (Opt("ntech.automation.creditcheck.disabled") ?? "false").ToLowerInvariant().Trim() == "true";
                return isCreditCheckAutomationEnabledOnClient && !isCreditCheckAutomatinDisabledByAppsetting;
            }
        }

        public static bool UseUcBvTestData => (Opt("ntech.precredit.useucbvtestdata") ?? "false")?.ToLowerInvariant()?.Trim() == "true";

        public static Tuple<string, string> ApplicationAutomationUsernameAndPassword => Tuple.Create(Req("ntech.automationuser.username"), Req("ntech.automationuser.password"));

        public static bool IsApplicationAutomationUsernameAndPasswordDefined => Opt("ntech.automationuser.username") != null && Opt("ntech.automationuser.password") != null;

        public static bool IsCreditManagementMonitorDisabled => OptBool("ntech.precredit.creditmanagementmonitor.disabled");

        public static int MaxAllowedArchiveLevel => int.Parse(Opt("ntech.precredit.maxallowedarchivelevel") ?? "1");

        public static int? MaxArchivedCountPerRun
        {
            get
            {
                var m = Opt("ntech.precredit.maxarchivecountperrun");
                if (m == null)
                    return null;
                return int.Parse(m);
            }
        }

        public static NTechSimpleSettings OutgoingPaymentFilesDanskeBankSettings
        {
            get
            {
                var f = E.StaticResourceFile("ntech.outgoingpayments.danskebankfi.settingsfile", "danskebank-fi-settings.txt", true);
                return NTechSimpleSettings.ParseSimpleSettingsFile(f.FullName, forceFileExistance: true);
            }
        }

        public static decimal? LegalInterestCeilingPercent
        {
            get
            {
                var c = Opt("ntech.legalinterestceilingpercent") ?? ClientCfg.OptionalSetting("ntech.legalinterestceilingpercent");
                if (c != null)
                    return decimal.Parse(c, CultureInfo.InvariantCulture);
                else
                    return null;
            }
        }

        public static DirectoryInfo PdfTemplateFolder => E.ClientResourceDirectory("ntech.pdf.templatefolder", "PdfTemplates", true);

        public static XDocument KycQuestions
        {
            get
            {
                return NTechCache.WithCache(
                    "0b6s5c13sdb-ae93-478f-b53b-b3e6bdaeff8e2",
                    TimeSpan.FromHours(4),
                    () =>
                        XDocuments.Load(
                            E.ClientResourceFile("ntech.credit.kycquestionsfile", "KycQuestions.xml", true).FullName));
            }
        }

        public static NTechServiceRegistry ServiceRegistry
        {
            get
            {
                return NTechCache.WithCache(
                    "199a098b-b4cb-4884-a1b4-061fa250d72c0",
                    TimeSpan.FromMinutes(5),
                    () => NTechEnvironment.Instance.ServiceRegistry);
            }
        }

        public static bool IsVerboseLoggingEnabled => (Opt("ntech.isverboseloggingenabled") ?? "false") == "true";

        public static CampaignCodeSettingsModel CampaignCodeSettings => envSettings.Value.CampaignCodeSettings;

        public class EncryptionKeySet
        {
            public string CurrentKeyName { get; set; }

            public List<KeyItem> AllKeys { get; set; }

            public string GetKey(string name)
            {
                return AllKeys.Single(x => x.Name == name).Key;
            }

            public Dictionary<string, string> AsDictionary()
            {
                return AllKeys.ToDictionary(x => x.Name, x => x.Key);
            }

            public class KeyItem
            {
                public string Name { get; set; }
                public string Key { get; set; }
            }
        }

        public static EncryptionKeySet EncryptionKeys
        {
            get
            {
                var file = E.StaticResourceFile("ntech.encryption.keysfile", "encryptionkeys.txt", true);
                return JsonConvert.DeserializeObject<EncryptionKeySet>(File.ReadAllText(file.FullName));
            }
        }

        public static string AdditionalQuestionsUrlPattern => envSettings.Value.AdditionalQuestionsUrlPattern;

        public static string ApplicationWrapperUrlPattern => envSettings.Value.ApplicationWrapperUrlPattern;

        public static bool ShowDemoMessages => envSettings.Value.ShowDemoMessages;

        public static DirectoryInfo SkinningRootFolder => E.ClientResourceDirectory("ntech.skinning.rootfolder", "Skinning", false);

        public static FileInfo SkinningCssFile => E.ClientResourceFile("ntech.skinning.cssfile", Path.Combine(SkinningRootFolder.FullName, "css\\skinning.css"), false);

        public static bool IsSkinningEnabled => NTechCache.WithCacheS($"ntech.cache.skinningenabled", TimeSpan.FromMinutes(5), () => NEnv.SkinningRootFolder?.Exists ?? false);

        public static bool IsSkinningCssEnabled => NTechCache.WithCacheS($"ntech.cache.skinningcssenabled", TimeSpan.FromMinutes(5), () => NEnv.SkinningCssFile?.Exists ?? false);

        public static int CreditApplicationWorkListIsNewMinutes => int.Parse(Opt("ntech.precredit.worklist.isnewminutes") ?? "3");

        public static string TestingOverrideDateFile
        {
            get
            {
                if (IsProduction)
                    return null;

                return E.StaticResourceFile("ntech.credit.testing.overridedatefile", "TestOverrideDate.txt", false).FullName;
            }
        }

        public static bool IsMortgageLoansEnabled => envSettings.Value.IsMortgageLoansEnabled;
        public static bool IsStandardMortgageLoansEnabled => envSettings.Value.IsStandardMortgageLoansEnabled;
        public static bool IsOnlyNonStandardMortgageLoansEnabled => envSettings.Value.IsOnlyNonStandardMortgageLoansEnabled;
        public static bool IsUnsecuredLoansEnabled => envSettings.Value.IsUnsecuredLoansEnabled;
        public static bool IsStandardUnsecuredLoansEnabled => envSettings.Value.IsStandardUnsecuredLoansEnabled;
        public static bool IsCompanyLoansEnabled => envSettings.Value.IsCompanyLoansEnabled;

        public static MortgageLoanNotificationSettingsModel MortgageLoanNotificationSettings
        {
            get
            {
                return NTechCache.WithCache("12f88240-2bb8-4220-be06-aa8a30128ff9", TimeSpan.FromMinutes(15), () =>
                {
                    var f = E.ClientResourceFile("ntech.credit.mortgageloan.notificationsettingsfile", "MortgageLoan-NotificationSettings.json", true);
                    return JsonConvert.DeserializeObject<MortgageLoanNotificationSettingsModel>(File.ReadAllText(f.FullName));
                });
            }
        }

        public static WorkflowModel MortgageLoanWorkflow
        {
            get
            {
                return NTechCache.WithCache("c54935ce-fb97-4244-b895-e3b5019fffa7", TimeSpan.FromMinutes(15), () =>
                {
                    if (!NEnv.IsMortgageLoansEnabled)
                        throw new Exception("Mortgage loan not enabled");
                    var f = E.ClientResourceFile("ntech.credit.mortgageloan.workflowfile", "MortgageLoan-Workflow.json", true);
                    return JsonConvert.DeserializeObject<WorkflowModel>(File.ReadAllText(f.FullName));
                });
            }
        }

        public static WorkflowModel CompanyLoanWorkflow
        {
            get
            {
                return NTechCache.WithCache("6fa0ecd1-6996-433f-a677-dbe4bca30c44", TimeSpan.FromMinutes(15), () =>
                {
                    if (!IsCompanyLoansEnabled)
                        throw new Exception("Company loan not enabled");
                    var f = E.ClientResourceFile("ntech.credit.companyloan.workflowfile", "CompanyLoan-Workflow.json", true);
                    return JsonConvert.DeserializeObject<WorkflowModel>(File.ReadAllText(f.FullName));
                });
            }
        }

        public static WorkflowModel UnsecuredLoanStandardWorkflow
        {
            get
            {
                if (!NEnv.IsStandardUnsecuredLoansEnabled)
                    throw new Exception("Unsecured loan not enabled");
                return UnsecuredLoanStandardWorkflowService.StandardWorkflow;
            }
        }

        public static WorkflowModel MortgageLoanStandardWorkflow
        {
            get
            {
                if (!IsStandardMortgageLoansEnabled)
                    throw new Exception("Mortgage loan not enabled");
                return MortgageLoanStandardWorkflowService.StandardWorkflow;
            }
        }

        public static bool IsTemplateCacheDisabled => string.Equals((Opt("ntech.precredit.disabletemplatecache") ?? "false"), "true", StringComparison.InvariantCultureIgnoreCase);

        public static int PersonCreditReportReuseDays => envSettings.Value.PersonCreditReportReuseDays;

        public static int CompanyCreditReportReuseDays => envSettings.Value.CompanyCreditReportReuseDays;

        private static NTechEnvironment E => envSettings.Value.E;
        private static string Opt(string n) => envSettings.Value.Opt(n);
        public static bool OptBool(string name) => envSettings.Value.OptBool(name);
        private static string Req(string n) => envSettings.Value.Req(n);

        public static bool IsNewFinalCreditCheckAllowedWhenBindingAgreementIsActive => OptBool("ntech.precredit.mortgageloan.allownewfinalwithactiveagreement");

        public static bool IsTranslationCacheDisabled => envSettings.Value.IsTranslationCacheDisabled;

        public static string GetMortageLoanLeftToLiveOnFileContent()
        {
            return File.ReadAllText(E.ClientResourceFile("ntech.precredit.mlltlfile", "MortgageLoan-LeftToLiveOn.json", true).FullName);
        }

        public static bool IsScoringVersionFieldHidden => OptBool("ntech.precredit.hidescoringversionfield");

        public static FileInfo CreditApplicationCustomEditableFieldsFile => E.ClientResourceFile("ntech.precredit.customfieldsfile", "CreditApplicationCustomEditableFields.json", false);

        public static bool CreditsUse360DayInterestYear => envSettings.Value.CreditsUse360DayInterestYear;

        public static TimeSpan MaxCustomerArchiveJobRuntime =>
            TimeSpan.FromMinutes(int.Parse(Opt("ntech.precredit.archivejob.maxtimeinminutes") ?? "5"));

        public static int CustomerArchiveJobBatchSize =>
            int.Parse(Opt("ntech.precredit.archivejob.batchsize") ?? "100");
    }

    public class LegacyPreCreditEnvSettings : IPreCreditEnvSettings
    {
        public int CreditApplicationWorkListIsNewMinutes => int.Parse(Opt("ntech.precredit.worklist.isnewminutes") ?? "3");
        public string DefaultScoringVersion => Opt("ntech.precredit.scoringversion") ?? ClientCfg.OptionalSetting("ntech.precredit.scoringversion");
        public ClientConfiguration ClientCfg
        {
            get
            {
                return NTechCache.WithCache("nPreCredit.ClientCfg", TimeSpan.FromMinutes(15), () => ClientConfiguration.CreateUsingNTechEnvironment());
            }
        }

        public bool IsMortgageLoansEnabled => ClientCfg.IsFeatureEnabled("ntech.feature.mortgageloans");
        public bool IsStandardMortgageLoansEnabled => IsMortgageLoansEnabled && ClientCfg.IsFeatureEnabled("ntech.feature.mortgageloans.standard");

        public bool IsOnlyNonStandardMortgageLoansEnabled => IsMortgageLoansEnabled && !IsStandardMortgageLoansEnabled;

        public bool IsUnsecuredLoansEnabled => ClientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans");
        public bool IsStandardUnsecuredLoansEnabled => IsUnsecuredLoansEnabled && ClientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans.standard");

        public bool IsCompanyLoansEnabled
        {
            get
            {
                var v = Opt("ntech.feature.companyloans");
                if (!string.IsNullOrWhiteSpace(v))
                    return v?.ToLowerInvariant() == "true";
                return ClientCfg.IsFeatureEnabled("ntech.feature.companyloans");
            }
        }

        public bool IsProduction
        {
            get
            {
                var s = Req("ntech.isproduction");
                return s.Trim().ToLower() == "true";
            }
        }

        private AffiliateModel GetAffiliateModelNonCached(string providerName, bool allowMissing)
        {
            var path = AffiliatesFolder;
            var affilateFile = Path.Combine(path.FullName, providerName + ".json");

            if (!File.Exists(affilateFile))
            {
                if (allowMissing)
                    return null;
                else
                    throw new Exception("Missing affiliate file: " + affilateFile);
            }

            return JsonConvert.DeserializeObject<AffiliateModel>(File.ReadAllText(affilateFile));
        }
        public DirectoryInfo AffiliatesFolder => E.ClientResourceDirectory("ntech.credit.affiliatesfolder", "Affiliates", true);

        public AffiliateModel GetAffiliateModel(string providerName, bool allowMissing = false)
        {
            return NTechCache.WithCache($"527332db-fb47-4dab-bd69-93088f3c9e95.{providerName}", TimeSpan.FromMinutes(15), () => new
            {
                Affilate = GetAffiliateModelNonCached(providerName, allowMissing: allowMissing)
            })?.Affilate;
        }

        public List<AffiliateModel> GetAffiliateModels()
        {
            return NTechCache.WithCache(
                "842a117c-99e9-4afd-95e5-454e573ec9ce",
                TimeSpan.FromMinutes(5),
                () => Directory
                    .GetFiles(AffiliatesFolder.FullName, "*.json")
                    .Select(x => JsonConvert.DeserializeObject<AffiliateModel>(File.ReadAllText(x)))
                    .ToList());
        }

        public bool OptBool(string name)
        {
            return (Opt(name) ?? "false").Trim().ToLowerInvariant() == "true";
        }

        public string Opt(string n)
        {
            return E.Setting(n, false);
        }

        public string Req(string n)
        {
            return E.Setting(n, true);
        }

        public NTechEnvironment E => NTechEnvironment.Instance;

        public bool IsTemplateCacheDisabled => string.Equals((Opt("ntech.document.disabletemplatecache") ?? "false"), "true", StringComparison.InvariantCultureIgnoreCase);
        public string CreditReportProviderName => Opt("ntech.credit.creditreportprovider") ?? "bisnodefi";

        /// <summary>
        /// Which creditreportproviders that we should list creditreports from in the system. 
        /// Comma-separated string, or will return the creditreportprovider if not set. 
        /// </summary>
        public string[] ListCreditReportProviders =>
            Opt("ntech.precredit.listcreditreportproviders")?.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray()
            ?? new[] { CreditReportProviderName };

        public bool CreditsUse360DayInterestYear => (ClientCfg.OptionalSetting("ntech.credit.interestmodel") ?? "") == "Actual_360";
        public string CurrentServiceName => "nPreCredit";

        public CampaignCodeSettingsModel CampaignCodeSettings
        {
            get
            {
                return new CampaignCodeSettingsModel
                {
                    DisableForceManualControl = (Opt("ntech.campaigncode.forcemanualcontrol.disable") ?? "").Trim().ToLowerInvariant() == "true",
                    DisableRemoveInitialFee = (Opt("ntech.campaigncode.removeinitialfee.disable") ?? "").Trim().ToLowerInvariant() == "true",
                };
            }
        }

        public ScoringSetupModel ScoringSetup
        {
            get
            {
                return NTechCache.WithCache("b57cb07c-c0fe-4a22-9502-5107fab8a5d3", TimeSpan.FromMinutes(15), () =>
                {
                    var f = E.ClientResourceFile("ntech.precredit.scoringsetupfile", "PreCredit-ScoringSetup.xml", true);

                    return ScoringSetupModel.Parse(
                        XDocuments.Load(f.FullName));
                });
            }
        }

        public int PersonCreditReportReuseDays => int.Parse(Opt("ntech.precredit.personcreditreportreusedays") ?? "1");

        public int CompanyCreditReportReuseDays => int.Parse(Opt("ntech.precredit.companycreditreportreusedays") ?? "7");

        public string AdditionalQuestionsUrlPattern
        {
            get
            {
                //Something like: http://localhost:32730/additional-questions?id={token}
                return Req("ntech.credit.additionalquestions.urlpattern");
            }
        }

        public string ApplicationWrapperUrlPattern => Opt("ntech.credit.applicationwrapper.urlpattern");

        public bool ShowDemoMessages => (Opt("ntech.precredit.showdemomessages") ?? "false").ToLowerInvariant() == "true";
        public SignatureProviderCode? SignatureProvider
        {
            get
            {
                var s = Opt("ntech.eidsignatureprovider")?.Trim()?.ToLowerInvariant();
                if (s == null)
                    return null;
                return (SignatureProviderCode)Enum.Parse(typeof(SignatureProviderCode), s, true);
            }
        }

        public bool IsAdditionalLoanScoringRuleDisabled => OptBool("ntech.precredit.isAdditionalLoanScoringRuleDisabled");
        public bool IsCoApplicantScoringRuleDisabled => OptBool("ntech.precredit.isCoApplicantScoringRuleDisabled");
        public bool IsTranslationCacheDisabled => OptBool("ntech.precredit.disabletranslationcache");
        public DirectoryInfo LogFolder
        {
            get
            {
                var v = Opt("ntech.logfolder");
                if (v == null)
                    return null;
                return new DirectoryInfo(v);
            }
        }

        public List<string> DisabledScoringRuleNames
        {
            get
            {
                var v = Opt("ntech.precredit.disabledScoringRuleNames");
                if (v == null)
                    return new List<string>();
                return v.Replace(" ", "").Replace(";", ",").Split(',').ToList();
            }
        }
    }
}