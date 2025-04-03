using IdentityModel.Client;
using Newtonsoft.Json;
using nGccCustomerApplication.Code;
using nGccCustomerApplication.Controllers;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace nGccCustomerApplication
{
    public static class NEnv
    {
        public static bool IsProduction
        {
            get
            {
                var s = Req("ntech.isproduction");
                return s.Trim().ToLower() == "true";
            }
        }

        public static bool IsBundlingEnabled
        {
            get
            {
                var s = Opt("ntech.isbundlingenabled") ?? "true";
                return s.Trim().ToLower() == "true";
            }
        }

        public static bool RedirectOldApplication
        {
            get
            {
                return (Opt("ntech.gcccustomerapplication.redirectoldapplication") ?? "false") == "true";
            }
        }

        public static XDocument KycQuestions
        {
            get
            {
                return NTechCache.WithCache(
                    "9ac532a7-c38e-45c7-83ac-34676d9ebb9d",
                    TimeSpan.FromHours(4),
                    () => XDocuments.Load(
                        NTechEnvironment.Instance.ClientResourceFile("ntech.credit.kycquestionsfile", "KycQuestions.xml", true).FullName));
            }
        }

        public static Tuple<string, string> CreateApplicationCredentials
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Opt("ntech.gcccustomerapplication.precreditcredentials.username")))
                    return Tuple.Create(Req("ntech.gcccustomerapplication.precreditcredentials.username"), Req("ntech.gcccustomerapplication.precreditcredentials.password"));
                else
                    return Tuple.Create(Req("ntech.automationuser.username"), Req("ntech.automationuser.password"));
            }
        }
        public static Tuple<string, string> SystemUserCredentials
        {
            get
            {
                return Tuple.Create(Req("ntech.automationuser.username"), Req("ntech.automationuser.password"));
            }
        }
        public static bool IsCustomerPagesKycQuestionsEnabled => ClientCfg.IsFeatureEnabled("feature.nGccCustomerApplication.kyc");
        public static string GetSelfCachingSystemUserBearerToken()
        {
            return NTechCache.WithCache("9ac532c38e-45cads7-83ac-34676d9ebbdasd9d", TimeSpan.FromMinutes(5), () =>
            {
                var u = CreateApplicationCredentials;
                return NHttp.AquireSystemUserAccessTokenWithUsernamePassword(u.Item1, u.Item2, NEnv.ServiceRegistry.Internal.ServiceRootUri("nUser"));
            });
        }

        public static string SelfProviderName
        {
            get
            {
                return Req("ntech.gcccustomerapplication.providername");
            }
        }

        public static string EoneProviderName
        {
            get
            {
                return Req("ntech.gcccustomerapplication.eone.providername");
            }
        }

        public static string EtuaProviderName
        {
            get
            {
                return Req("ntech.gcccustomerapplication.etua.providername");
            }
        }

        public static string SalusProviderName
        {
            get
            {
                return Req("ntech.gcccustomerapplication.salus.providername");
            }
        }

        public static bool IsVerboseLoggingEnabled
        {
            get
            {
                return (Opt("ntech.isverboseloggingenabled") ?? "false") == "true";
            }
        }

        public static bool IsCachingEnabled
        {
            get
            {
                return (Opt("ntech.gcccustomerapplication.disablecaching") ?? "false").ToLowerInvariant() == "true";
            }
        }

        public static Uri SuccessUrl
        {
            get
            {
                var s = Opt("ntech.gcccustomerapplication.successurl");
                if (s == null)
                    return null;
                return new Uri(s);
            }
        }

        public static Uri FailedUrl
        {
            get
            {
                var s = Opt("ntech.gcccustomerapplication.failedurl");
                if (s == null)
                    return null;
                return new Uri(s);
            }
        }

        public static ISet<string> ProviderNamesWithDisabledAutomation
        {
            get
            {
                var s = Opt("ntech.gcccustomerapplication.providerswithdisabledautomation");
                if (s == null)
                    return new HashSet<string>();
                else
                    return new HashSet<string>(s.Split(',').Where(x => !string.IsNullOrWhiteSpace(x)));
            }
        }

        public static DirectoryInfo AffiliatesFolder
        {
            get
            {
                return NTechEnvironment.Instance.ClientResourceDirectory("ntech.credit.affiliatesfolder", "Affiliates", true);
            }
        }

        public static Affiliate GetAffiliateModel(string providerName, bool allowMissing = false)
        {
            return NTechCache.WithCache($"ntech.credit.affilate.{providerName}", TimeSpan.FromMinutes(15), () =>
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

                return JsonConvert.DeserializeObject<Affiliate>(File.ReadAllText(affilateFile));
            });
        }

        public static ClientConfiguration ClientCfg
        {
            get
            {
                return NTechCache.WithCache("nGccCustomerApplication.ClientCfg", TimeSpan.FromMinutes(15), () => ClientConfiguration.CreateUsingNTechEnvironment());
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

        public class Affiliate
        {
            public string ProviderName { get; set; }
            public string DisplayToEnduserName { get; set; }
            public string StreetAddress { get; set; }
            public string EnduserContactPhone { get; set; }
            public string EnduserContactEmail { get; set; }
            public string WebsiteAddress { get; set; }
            public bool IsSelf { get; set; }
            public bool IsSendingRejectionEmails { get; set; }
            public bool IsUsingDirectLinkFlow { get; set; }
            public bool? HasBrandedAdditionalQuestions { get; set; }
            public string BrandingTag { get; set; } //Default is to use the ProviderName
            public bool IsSendingAdditionalQuestionsEmail { get; set; }
            public bool IsMortgageLoanProvider { get; set; }
            public string MortgageLoanProviderIntegrationName { get; set; }
            public string FallbackCampaignCode { get; set; }
            public bool UsesStandardRequestFormat { get; set; }
        }

        private static string Opt(string n)
        {
            return NTechEnvironment.Instance.Setting(n, false);
        }

        private static string Req(string n)
        {
            return NTechEnvironment.Instance.Setting(n, true);
        }

        public static string ProviderApplicationLogFolder
        {
            get
            {
                return Opt("ntech.gcccustomerapplication.providerapplicationlogfolder");
            }
        }

        public static NTechServiceRegistry ServiceRegistry
        {
            get
            {
                return NTechEnvironment.Instance.ServiceRegistry;
            }
        }

        public static string CurrentServiceName => "nGccCustomerApplication";

        public static bool IsGccCustomerApplicationKycQuestionsEnabled => ClientCfg.IsFeatureEnabled("feature.gcccustomerapplication.kyc");


        public static bool IsMortgageLoansEnabled => ClientCfg.IsFeatureEnabled("ntech.feature.mortgageloans");

        public static bool IsStandardMortgageLoansEnabled => IsMortgageLoansEnabled && ClientCfg.IsFeatureEnabled("ntech.feature.mortgageloans.standard");

        public static bool IsUnsecuredLoansEnabled => ClientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans");

        public static string LogFolder => Req("ntech.logfolder");

        public static bool IsStandardUnsecuredLoansEnabled => IsUnsecuredLoansEnabled && ClientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans.standard");
        public static bool IsStandardMlOrUlEnabled => IsUnsecuredLoansEnabled || IsStandardMortgageLoansEnabled;
        public static bool IsConsumerCreditStandardProviderApiLoggingEnabled => (Opt("ntech.gcccustomerapplication.isconsumercreditstandardproviderapiloggingenabled") ?? "false")?.ToLowerInvariant() == "true";
        public static bool IsStandardEmbeddedGccCustomerApplicationEnabled => IsStandardMlOrUlEnabled;
        public static bool IsBackButtonPreventDisabled => NTechCache.WithCacheS("e8240e31-cd4a-4afb-b9c8-ce0a073f920b", TimeSpan.FromMinutes(5), () =>
            (Opt("ntech.gcccustomerapplication.disableBackButtonPrevent") ?? "false")?.ToLower() == "true");
        public static bool IsCreditOverviewActive => ClientCfg.IsFeatureEnabled("ntech.feature.gcccustomerapplication.creditoverview") && ServiceRegistry.ContainsService("nCredit");

        public static bool IsSavingsOverviewActive => ClientCfg.IsFeatureEnabled("ntech.feature.gcccustomerapplication.savingsoverview") && ServiceRegistry.ContainsService("nSavings");

        public static bool IsBalanzia => ClientCfg.ClientName.Equals("balanzia", StringComparison.OrdinalIgnoreCase);

        public static bool IsSavingsApplicationActive => ClientCfg.IsFeatureEnabled("ntech.feature.gcccustomerapplication.savingsapplication") && ServiceRegistry.ContainsService("nSavings");

        public static bool SkipInitialCompanyLoanScoring => Opt("ntech.gcccustomerapplication.skipinitialcompanyloanscoring")?.ToLowerInvariant()?.Trim() == "true";
        public static bool IsEmbeddedMortageLoanGccCustomerApplicationEnabled
        {
            get
            {
                if (!IsMortgageLoansEnabled)
                    return false;

                return ClientCfg.IsFeatureEnabled("ntech.gcccustomerapplication.embeddedmortageloancustomerpage") || (Opt("ntech.gcccustomerapplication.embeddedmortageloanGccCustomerApplicationenabled") == "true");
            }
        }

        private static Lazy<NTech.Banking.BankAccounts.Fi.IBANToBICTranslator> iBANToBICTranslatorInstance = new Lazy<NTech.Banking.BankAccounts.Fi.IBANToBICTranslator>(() => new NTech.Banking.BankAccounts.Fi.IBANToBICTranslator());

        public static NTech.Banking.BankAccounts.Fi.IBANToBICTranslator IBANToBICTranslatorInstance => iBANToBICTranslatorInstance.Value;

        public static NTech.Banking.CivicRegNumbers.CivicRegNumberParser BaseCivicRegNumberParser => NTechCache.WithCache("BaseCivicRegNumberParser", TimeSpan.FromMinutes(5), () => new NTech.Banking.CivicRegNumbers.CivicRegNumberParser(ClientCfg.Country.BaseCountry));


        public static bool IsTranslationCacheDisabled => (Opt("ntech.gcccustomerapplication.translation.cachedisabled") ?? "false") == "true";


        public static string TranslationOverrideEmbeddedFileWithThisLocalFilePath => Opt("ntech.gcccustomerapplication.translation.overridefilepath");
        private static NTechEnvironment E => NTechEnvironment.Instance;

        public static bool IsDirectEidAuthenticationModeEnabled =>
         E.IsFeatureEnabledWithAppSettingOverride("ntech.gcccustomerapplication.allowdirecteidlogin", ClientCfg);
        public static bool IsEmbeddedSiteEidLoginApiEnabled =>
        E.IsFeatureEnabledWithAppSettingOverride("ntech.gcccustomerapplication.allowembeddedsiteapieidlogin", ClientCfg);

        public static HashSet<string> AllTrackedExternalVariables
        {
            get
            {
                var h = new HashSet<string>();
                var s1 = SavingsAffiliateTrackingModelSettings;
                if (s1.IsEnabled)
                    s1.ExternalVariables.ToList().ForEach(x => h.Add(x));
                h.Add("cc"); //campaign code
                h.Add("pp"); //provider name
                return h;
            }
        }

        public static AffiliateTrackingModel.Settings SavingsAffiliateTrackingModelSettings
        {
            get
            {
                var f = E.StaticResourceFile("ntech.gcccustomerapplication.savingsaffiliatetrackingfile", "savings-affiliatetracking.txt", false);
                if (f.Exists)
                {
                    return AffiliateTrackingModel.CreateSettings(NTechSimpleSettings.ParseSimpleSettingsFile(f.FullName));
                }
                return new AffiliateTrackingModel.Settings
                {
                    IsEnabled = false
                };
            }
        }


        public static string SystemUserBearerToken
        {
            get
            {
                return NTechCache.WithCache("d88f8ffc-ed37-486f-9545-3a6f35d484db", TimeSpan.FromMinutes(3), () =>
                {
                    var tokenClient = new TokenClient(
                                            ServiceRegistry.Internal.ServiceUrl("nUser", "id/connect/token").ToString(),
                                            "nTechSystemUser",
                                            "nTechSystemUser");

                    var credentials = SystemUserUserNameAndPassword;
                    var token = tokenClient.RequestResourceOwnerPasswordAsync(credentials.Item1, credentials.Item2, scope: "nTech1").Result;

                    if (token.IsError)
                    {
                        throw new Exception("Bearer token login failed in nTest event automation :" + token.Error);
                    }

                    return token.AccessToken;
                });
            }
        }
        public static Tuple<string, string> SystemUserUserNameAndPassword
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Opt("ntech.gcccustomerapplication.systemuser.username")))
                    return Tuple.Create(Req("ntech.gcccustomerapplication.systemuser.username"), Req("ntech.gcccustomerapplication.systemuser.password"));
                else
                    return Tuple.Create(Req("ntech.automationuser.username"), Req("ntech.automationuser.password"));
            }
        }
        public static bool IsSkinningEnabled => NTechCache.WithCacheS($"ntech.cache.skinningenabled", TimeSpan.FromMinutes(5), () => NEnv.SkinningRootFolder?.Exists ?? false);
        public static DirectoryInfo SkinningRootFolder => E.ClientResourceDirectory("ntech.skinning.rootfolder", "Skinning", false);

        public static bool IsCreditTokenAuthenticationModeEnabled => IsCreditOverviewActive && ClientCfg.IsFeatureEnabled("ntech.feature.gcccustomerapplication.allowcredittokenlogin");

        public static IClientConfigurationCore ClientCfgCore =>
            NTechCache.WithCache("nGccCustomerApplication.ClientCfgCore", TimeSpan.FromMinutes(15), () => ClientConfigurationCoreFactory.CreateUsingNTechEnvironment(E));
        public static string NTechCdnUrl => Opt("ntech.cdn.rooturl");


    }
}