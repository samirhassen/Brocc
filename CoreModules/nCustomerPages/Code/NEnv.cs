using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using IdentityModel.Client;
using nCustomerPages.Code;
using nCustomerPages.Code.ElectronicIdSignature;
using Newtonsoft.Json;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.CivicRegNumbers;
using NTech.Banking.Conversion;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;

namespace nCustomerPages;

public static class NEnv
{
    public static bool IsProduction => Req("ntech.isproduction") == "true";

    public static string TestingOverrideDateFile =>
        IsProduction ? null : Opt("ntech.credit.testing.overridedatefile");

    public static bool IsCachingEnabled =>
        (Opt("ntech.iscachingenabled") ?? "true").ToLowerInvariant().Trim() == "true";

    public static bool IsSecureMessagesEnabled
    {
        get
        {
            const string Name = "ntech.feature.securemessages";
            if (ClientCfg.IsFeatureEnabled(Name))
                return true;

            return (Opt(Name) ?? "false").Trim().ToLowerInvariant() == "true";
        }
    }

    public static DirectoryInfo SkinningRootFolder =>
        E.ClientResourceDirectory("ntech.skinning.rootfolder", "Skinning", false);

    public static FileInfo SkinningCssFile => E.ClientResourceFile("ntech.skinning.cssfile",
        Path.Combine(SkinningRootFolder.FullName, "css\\skinning.css"), false);

    public static bool IsSkinningEnabled => NTechCache.WithCacheS($"ntech.cache.skinningenabled",
        TimeSpan.FromMinutes(5), () => SkinningRootFolder?.Exists ?? false);

    public static bool IsSkinningCssEnabled => NTechCache.WithCacheS($"ntech.cache.skinningcssenabled",
        TimeSpan.FromMinutes(5), () => SkinningCssFile?.Exists ?? false);

    public static XDocument KycQuestions => NTechCache.WithCache(
        "0b65c13b-ae93-478f-b53b-b3e6bdeff8e2",
        TimeSpan.FromHours(4),
        () => XDocuments.Load(
            E.ClientResourceFile("ntech.customerpages.kycquestionsfile", "KycQuestions.xml", true).FullName
        ));

    public static XDocument MortgageLoanKycQuestions
    {
        get
        {
            XDocument Load() => XDocuments.Load(E
                .ClientResourceFile("ntech.customerpages.mortgageloankycquestionsfile",
                    "MortgageLoan-KycQuestions.xml", true).FullName);

            if (IsProduction)
                return NTechCache.WithCache(
                    "fddb1870-8f26-40f6-b48a-511dc7a054c6",
                    TimeSpan.FromHours(4),
                    (Func<XDocument>)Load);

            return Load();
        }
    }

    public static NTechServiceRegistry ServiceRegistry
    {
        get
        {
            return NTechCache.WithCache("06eaf450-d780-41c4-9ec1-fc423bc8a1a3", TimeSpan.FromMinutes(15),
                () => E.ServiceRegistry);
        }
    }

    public static bool IsCreditTokenAuthenticationModeEnabled => IsCreditOverviewActive &&
                                                                 ClientCfg.IsFeatureEnabled(
                                                                     "ntech.feature.customerpages.allowcredittokenlogin");

    public static bool IsCreditOverviewActive =>
        ClientCfg.IsFeatureEnabled("ntech.feature.customerpages.creditoverview") &&
        ServiceRegistry.ContainsService("nCredit");

    public static bool IsSavingsOverviewActive =>
        ClientCfg.IsFeatureEnabled("ntech.feature.customerpages.savingsoverview") &&
        ServiceRegistry.ContainsService("nSavings");

    public static bool IsBalanzia => ClientCfg.ClientName.Equals("balanzia", StringComparison.OrdinalIgnoreCase);

    public static bool IsSavingsApplicationActive =>
        ClientCfg.IsFeatureEnabled("ntech.feature.customerpages.savingsapplication") &&
        ServiceRegistry.ContainsService("nSavings");

    public static bool SkipInitialCompanyLoanScoring =>
        Opt("ntech.customerpages.skipinitialcompanyloanscoring")?.ToLowerInvariant()?.Trim() == "true";

    public static bool IsEmbeddedMortageLoanCustomerPagesEnabled
    {
        get
        {
            if (!IsMortgageLoansEnabled)
                return false;

            return ClientCfg.IsFeatureEnabled("ntech.customerpages.embeddedmortageloancustomerpage") ||
                   Opt("ntech.customerpages.embeddedmortageloancustomerpagesenabled") == "true";
        }
    }

    public static ClientConfiguration ClientCfg => NTechCache.WithCache("nCustomerPages.ClientCfg",
        TimeSpan.FromMinutes(15), () => ClientConfiguration.CreateUsingNTechEnvironment());

    public static IClientConfigurationCore ClientCfgCore =>
        NTechCache.WithCache("nCustomerPages.ClientCfgCore", TimeSpan.FromMinutes(15),
            () => ClientConfigurationCoreFactory.CreateUsingNTechEnvironment(E));

    public static Tuple<string, string> SystemUserUserNameAndPassword
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Opt("ntech.customerpages.systemuser.username")))
                return Tuple.Create(Req("ntech.customerpages.systemuser.username"),
                    Req("ntech.customerpages.systemuser.password"));
            
            return Tuple.Create(Req("ntech.automationuser.username"), Req("ntech.automationuser.password"));
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
                var token = tokenClient
                    .RequestResourceOwnerPasswordAsync(credentials.Item1, credentials.Item2, scope: "nTech1")
                    .Result;

                if (token.IsError)
                {
                    throw new Exception("Bearer token login failed in nTest event automation :" + token.Error);
                }

                return token.AccessToken;
            });
        }
    }

    public static bool IsTranslationCacheDisabled =>
        (Opt("ntech.customerpages.translation.cachedisabled") ?? "false") == "true";

    public static string TranslationOverrideEmbeddedFileWithThisLocalFilePath =>
        Opt("ntech.customerpages.translation.overridefilepath");

    public static bool IsBundlingEnabled
    {
        get
        {
            var s = Opt("ntech.isbundlingenabled") ?? "true";
            return s.Trim().ToLower() == "true";
        }
    }

    public static bool IsCompanyLoansEnabled
    {
        get
        {
            var v = Opt("ntech.feature.companyloans");
            if (!string.IsNullOrWhiteSpace(v))
                return v.ToLowerInvariant() == "true";
            return ClientCfg.IsFeatureEnabled("ntech.feature.companyloans");
        }
    }

    public static AffiliateTrackingModel.Settings SavingsAffiliateTrackingModelSettings
    {
        get
        {
            var f = E.StaticResourceFile("ntech.customerpages.savingsaffiliatetrackingfile",
                "savings-affiliatetracking.txt", false);
            if (f.Exists)
            {
                return AffiliateTrackingModel.CreateSettings(
                    NTechSimpleSettings.ParseSimpleSettingsFile(f.FullName));
            }

            return new AffiliateTrackingModel.Settings
            {
                IsEnabled = false
            };
        }
    }

    public static bool IsMortgageLoanProviderApiEnabled =>
        ClientCfg.IsFeatureEnabled("ntech.feature.mortgageloan.customerpagesproviderapi");

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

    public static string NTechCdnUrl => Opt("ntech.cdn.rooturl");

    private static Lazy<IBANToBICTranslator> iBANToBICTranslatorInstance = new(() => new IBANToBICTranslator());

    public static IBANToBICTranslator IBANToBICTranslatorInstance => iBANToBICTranslatorInstance.Value;

    private static NTechEnvironment E => NTechEnvironment.Instance;

    private static string Opt(string n)
    {
        return E.Setting(n, false);
    }

    private static string Req(string n)
    {
        return E.Setting(n, true);
    }

    public static bool IsVerboseLoggingEnabled => (Opt("ntech.isverboseloggingenabled") ?? "false") == "true";

    public static bool IsMortgageLoanProviderApiLoggingEnabled =>
        (Opt("ntech.customerpages.isproviderapiloggingenabled") ?? "false").ToLowerInvariant() == "true";

    public static CivicRegNumberParser BaseCivicRegNumberParser => NTechCache.WithCache("BaseCivicRegNumberParser",
        TimeSpan.FromMinutes(5), () => new CivicRegNumberParser(ClientCfg.Country.BaseCountry));

    public static DirectoryInfo PdfTemplateFolder =>
        E.ClientResourceDirectory("ntech.pdf.templatefolder", "PdfTemplates", true);

    public static FileInfo SavingsAccountWithdrawalAccountChangeAgreementFilePath => E.ClientResourceFile(
        "ntech.customerpages.Savingsaccountwithdrawalcccountchangeagreementpdf",
        "SavingsAccountWithdrawalAccountChangeAgreement.pdf", true);

    public static DirectoryInfo AffiliatesFolder =>
        E.ClientResourceDirectory("ntech.customerpages.affiliatesfolder", "Affiliates", true);

    public static Affiliate GetAffiliateModel(string providerName, bool allowMissing = false)
    {
        return NTechCache.WithCache($"ntech.customerpages.affilate.{providerName}", TimeSpan.FromMinutes(15), () =>
        {
            var path = AffiliatesFolder;
            var affiliateFile = Path.Combine(path.FullName, providerName + ".json");

            if (File.Exists(affiliateFile))
                return JsonConvert.DeserializeObject<Affiliate>(File.ReadAllText(affiliateFile));
            if (allowMissing)
                return null;

            throw new Exception("Missing affiliate file: " + affiliateFile);
        });
    }

    public static List<Affiliate> GetAffiliateModels()
    {
        return NTechCache.WithCache(
            "ntech.customerpages.affilates",
            TimeSpan.FromMinutes(5),
            () => Directory
                .GetFiles(AffiliatesFolder.FullName, "*.json")
                .Select(x => JsonConvert.DeserializeObject<Affiliate>(File.ReadAllText(x)))
                .ToList());
    }

    public static bool IsCustomerPagesKycQuestionsEnabled =>
        ClientCfg.IsFeatureEnabled("feature.customerpages.kyc");

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
        public bool UsesStandardRequestFormat { get; set; }
    }

    public static bool IsMortgageLoansEnabled => ClientCfg.IsFeatureEnabled("ntech.feature.mortgageloans");

    public static bool IsStandardMortgageLoansEnabled => IsMortgageLoansEnabled &&
                                                         ClientCfg.IsFeatureEnabled(
                                                             "ntech.feature.mortgageloans.standard");

    public static bool IsUnsecuredLoansEnabled => ClientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans");

    public static string LogFolder => Req("ntech.logfolder");

    public static ISet<string> ProviderNamesWithDisabledAutomation
    {
        get
        {
            var s = Opt("ntech.customerpages.providerswithdisabledautomation");
            return s == null
                ? new HashSet<string>()
                : new HashSet<string>(s.Split(',').Where(x => !string.IsNullOrWhiteSpace(x)));
        }
    }

    public static ISet<string> ProviderNamesWithDisabledDirectScoring
    {
        get
        {
            var s = Opt("ntech.customerpages.providerswithdisableddirectscoring");
            return s == null
                ? new HashSet<string>()
                : new HashSet<string>(s.Split(',').Where(x => !string.IsNullOrWhiteSpace(x)));
        }
    }

    public static ElectronicIdProviderCode SignatureElectronicIdProviderCode
    {
        get
        {
            var providerName = Opt("ntech.eidsignatureprovider");
            var providerCode = Enums.Parse<ElectronicIdProviderCode>(providerName, ignoreCase: true);
            return providerCode ?? ElectronicIdProviderCode.None;
        }
    }

    public static bool IsDirectEidAuthenticationModeEnabled =>
        E.IsFeatureEnabledWithAppSettingOverride("ntech.customerpages.allowdirecteidlogin", ClientCfg);


    public static bool IsEmbeddedSiteEidLoginApiEnabled =>
        E.IsFeatureEnabledWithAppSettingOverride("ntech.customerpages.allowembeddedsiteapieidlogin", ClientCfg);

    public static string CurrentServiceName => "nCustomerPages";

    public static bool IsStandardUnsecuredLoansEnabled => IsUnsecuredLoansEnabled &&
                                                          ClientCfg.IsFeatureEnabled(
                                                              "ntech.feature.unsecuredloans.standard");

    public static bool IsStandardMlOrUlEnabled => IsUnsecuredLoansEnabled || IsStandardMortgageLoansEnabled;

    public static bool IsConsumerCreditStandardProviderApiLoggingEnabled =>
        (Opt("ntech.customerpages.isconsumercreditstandardproviderapiloggingenabled") ?? "false")
        .ToLowerInvariant() == "true";

    public static bool IsStandardEmbeddedCustomerPagesEnabled => IsStandardMlOrUlEnabled;

    public static bool IsBackButtonPreventDisabled => NTechCache.WithCacheS("e8240e31-cd4a-4afb-b9c8-ce0a073f920b",
        TimeSpan.FromMinutes(5), () =>
            (Opt("ntech.customerpages.disableBackButtonPrevent") ?? "false").ToLower() == "true");
}