using Duende.IdentityModel.Client;
using nCustomer.Code.Services.Aml.Cm1;
using Newtonsoft.Json;
using NTech.Banking.CivicRegNumbers;
using NTech.Banking.OrganisationNumbers;
using NTech.Core.Customer.Shared;
using NTech.Core.Customer.Shared.Models;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;

namespace nCustomer
{
    public static class NEnv
    {
        public static string CurrentServiceName => "nCustomer";

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

        public static bool IsBundlingEnabled
        {
            get
            {
                var s = Opt("ntech.isbundlingenabled") ?? "true";
                return s.Trim().ToLower() == "true";
            }
        }

        public static string NTechCdnUrl => Opt("ntech.cdn.rooturl");

        public static bool IsMortgageLoansEnabled
        {
            get
            {
                var isUnsecuredLoansEnabled = ClientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans");
                var isMortgageLoansEnabled = ClientCfg.IsFeatureEnabled("ntech.feature.mortgageloans");

                if (isUnsecuredLoansEnabled && isMortgageLoansEnabled)
                    throw new Exception("Cannot have both mortgage loans and unsecured loans enabled at the same time in this module");

                return isMortgageLoansEnabled;
            }
        }

        public static bool IsCompanyLoansEnabled
        {
            get
            {
                var v = Opt("ntech.feature.companyloans");
                if (!string.IsNullOrWhiteSpace(v))
                    return v?.ToLowerInvariant() == "true";
                return ClientCfg.IsFeatureEnabled("ntech.feature.companyloans");
            }
        }

        public static bool IsUnsecuredLoansEnabled
        {
            get
            {
                var isUnsecuredLoansEnabled = ClientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans");
                var isMortgageLoansEnabled = ClientCfg.IsFeatureEnabled("ntech.feature.mortgageloans");

                if (isUnsecuredLoansEnabled && isMortgageLoansEnabled)
                    throw new Exception("Cannot have both mortgage loans and unsecured loans enabled at the same time in this module");

                return isUnsecuredLoansEnabled;
            }
        }

        public static bool IsStandardUnsecuredLoansEnabled => IsUnsecuredLoansEnabled && ClientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans.standard");

        public static string SignatureProvider => Opt("ntech.eidsignatureprovider")?.Trim()?.ToLowerInvariant();

        public static NTechSimpleSettings ScriveSignatureSettings
        {
            get
            {
                var f = E.StaticResourceFile("ntech.scrive.signature.settingsfile", "scrive-signature-settings.txt", true);
                return NTechSimpleSettings.ParseSimpleSettingsFile(f.FullName);
            }
        }

        public static NTechSimpleSettings AssentlySignatureSettings
        {
            get
            {
                var f = E.StaticResourceFile("ntech.assently.signature.settingsfile", "assently-signature-settings.txt", true);
                return NTechSimpleSettings.ParseSimpleSettingsFile(f.FullName);
            }
        }

        public static NTechSimpleSettings Signicat2SignatureSettings
        {
            get
            {
                var f = E.StaticResourceFile("ntech.signicat2.signature.settingsfile", "signicat2-signature-settings.txt", true);
                return NTechSimpleSettings.ParseSimpleSettingsFile(f.FullName);
            }
        }

        public static string EidLoginProvider => Opt("ntech.eidloginprovider")?.Trim()?.ToLowerInvariant();

        public static NTechSimpleSettings ScriveAuthenticationSettings
        {
            get
            {
                var f = E.StaticResourceFile("ntech.scrive.authentication.settingsfile", "scrive-authentication-settings.txt", true);
                return NTechSimpleSettings.ParseSimpleSettingsFile(f.FullName);
            }
        }

        public static NTechSimpleSettings Signicat2AuthenticationSettings
        {
            get
            {
                var f = E.StaticResourceFile("ntech.signicat2.authentication.settingsfile", "signicat2-authentication-settings.txt", true);
                return NTechSimpleSettings.ParseSimpleSettingsFile(f.FullName);
            }
        }

        public static Dictionary<string, KycQuestionsTemplate> DefaultKycQuestionsSets
        {
            get
            {
                var f = E.ClientResourceFile("ntech.kyc.ui.questionsset", "kyc-questions.json", false);
                return NTechCache.WithCache("6da5304c-49ef-43c5-aa30-3a9849148f83", TimeSpan.FromMinutes(15), () => f.Exists
                    ? KycQuestionsTemplate.ParseDefaultSetting(File.ReadAllText(f.FullName))
                    : new Dictionary<string, KycQuestionsTemplate>()
                );
            }
        }

        public static DirectoryInfo LogFolder
        {
            get
            {
                var v = Opt("ntech.logfolder");
                if (v == null)
                    return null;
                return new DirectoryInfo(v);
            }
        }

        public static ClientConfiguration ClientCfg
        {
            get
            {
                return NTechCache.WithCache("nCustomer.ClientCfg", TimeSpan.FromMinutes(15), () => ClientConfiguration.CreateUsingNTechEnvironment());
            }
        }

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

        public static bool IsProduction
        {
            get
            {
                var s = Req("ntech.isproduction");
                return s.Trim().ToLower() == "true";
            }
        }

        public static NTechServiceRegistry ServiceRegistry
        {
            get
            {
                return NTechCache.WithCache(
                    "27df9886-27d9-43b7-9484-cb44d7cc3f4f0",
                    TimeSpan.FromMinutes(5),
                    () => E.ServiceRegistry);
            }
        }

        public static bool IsVerboseLoggingEnabled => OptBool("ntech.isverboseloggingenabled");

        public static bool IsTranslationCacheDisabled => OptBool("ntech.customer.disabletranslationcache");

        public static bool OptBool(string name) => (Opt(name) ?? "false").Trim().ToLowerInvariant() == "true";

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

        public static EncryptionKeySet EncryptionKeys =>
            JsonConvert.DeserializeObject<EncryptionKeySet>(File.ReadAllText(
                E.StaticResourceFile("ntech.encryption.keysfile", "encryptionkeys.txt", true).FullName));

        public static XDocument FatcaTemplateFile => XDocuments.Load(E.ClientResourceFile("ntech.fatca.templatefile", "FATCA-Template.xml", true).FullName);

        public static Tuple<string, string> ApplicationAutomationUsernameAndPassword => Tuple.Create(Req("ntech.automationuser.username"), Req("ntech.automationuser.password"));

        public static bool IsApplicationAutomationUsernameAndPasswordDefined => Opt("ntech.automationuser.username") != null && Opt("ntech.automationuser.password") != null;

        public static string AquireApplicationAutomationUsernameAndPasswordBearerToken()
        {
            return NTechCache.WithCache("nCustomerEventAutomation.95ed3beb-1593-4b54-94b2-760a32e640cd", TimeSpan.FromMinutes(3), () =>
            {
             

                var client = new HttpClient();
                var credentials = NEnv.ApplicationAutomationUsernameAndPassword;
                var token = client.RequestPasswordTokenAsync(new PasswordTokenRequest()
                {
                    Address = NEnv.ServiceRegistry.Internal.ServiceUrl("nUser", "id/connect/token").ToString(),
                    ClientId = "nTechSystemUser",
                    ClientSecret = "nTechSystemUser",
                    UserName = credentials.Item1,
                    Password = credentials.Item2,
                    Scope = "nTech1"
                });

                if (token.Result.IsError)
                {
                    throw new Exception("Bearer token login failed in nPreCredit event automation :" + token.Result.Error);
                }

                return token.Result.AccessToken;
            });
        }

        public static Cm1KycSettings Cm1Kyc
        {
            get
            {
                var f = E.StaticResourceFile("ntech.customer.cm1settingsfile", "cm1-kyc-settings.txt", false);
                if (!f.Exists)
                    return null;
                var s = NTechSimpleSettings.ParseSimpleSettingsFile(f.FullName, forceFileExistance: true);
                return new Cm1KycSettings
                {
                    Disabled = s.OptBool("disabled"),
                    Endpoint = s.Req("endpointUrl"),
                    ClientCertificateFilePath = s.Opt("clientCertificateFilePath"),
                    ClientCertificateFilePassword = s.Opt("clientCertificateFilePassword"),
                    ClientCertificateThumbprint = s.Opt("clientCertificateThumbprint"),
                    XIdentifier = s.Req("xIdentifier"),
                    DebugLogFolder = s.Opt("debugLogFolder"),
                    QualityCutoff = int.Parse(s.Req("qualityCutoff")),
                    ForceDisableScreenOnly = s.OptBool("forceDisableScreenOnly")
                };
            }
        }

        public static Cm1FtpSettings Cm1FtpSettings
        {
            get
            {
                var cm1FileInfo = E.StaticResourceFile("ntech.cm1.ftpsettingsfile", "cm1-ftp-settings.txt", false);
                if (!(cm1FileInfo?.Exists ?? false))
                    return null;

                var settings = NTechSimpleSettings.ParseSimpleSettingsFile(cm1FileInfo.FullName, forceFileExistance: true);
                return new Cm1FtpSettings
                {
                    Enabled = settings.OptBool("enabled"),
                    HostName = settings.Req("cm1.ftp.hostname"),
                    UserName = settings.Req("cm1.ftp.user"),
                    PrivateKeyPathway = settings.Opt("cm1.ftp.privatekeypathway"),
                    PrivateKeyPassword = settings.Opt("cm1.ftp.privatekeypassword"),
                    Password = settings.Opt("cm1.ftp.password"),
                    Port = Convert.ToInt32(settings.Req("cm1.ftp.port")),
                    FoldersToScan = settings.Req("cm1.ftp.folderstoscan").Split(',').ToList(),
                    FileNamePattern = settings.Req("cm1.ftp.filenamepattern"),
                };
            }
        }

        public static NTechSimpleSettings TrapetsKycInstantWatchRest
        {
            get
            {
                var f = E.StaticResourceFile("ntech.trapetskyc.restsettings", "trapets-kyc-rest-settings.txt", false);
                if (!f.Exists)
                    return null;
                return NTechSimpleSettings.ParseSimpleSettingsFile(f.FullName, forceFileExistance: true);
            }
        }

        public class TrapetsKycSoapInstantWatchSettings
        {
            public string Endpoint { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string DebugLogFolder { get; set; }
            public bool SkipSoundex { get; set; }
            public string SpecialSingleQueryUsername { get; set; }
            public string SpecialSingleQueryPassword { get; set; }
        }

        public static TrapetsKycSoapInstantWatchSettings TrapetsKycInstantWatchSoap
        {
            get
            {
                var un = Opt("ntech.trapets.kycinstantwatch.username");
                var pw = Opt("ntech.trapets.kycinstantwatch.password");
                var endpoint = Opt("ntech.trapets.kycinstantwatch.serviceendpoint");
                if (un == null || pw == null || endpoint == null)
                {
                    return null;
                }
                return new TrapetsKycSoapInstantWatchSettings
                {
                    Username = un,
                    Password = pw,
                    SpecialSingleQueryPassword = Opt("ntech.trapets.kycinstantwatch.single.password"),
                    SpecialSingleQueryUsername = Opt("ntech.trapets.kycinstantwatch.single.username"),
                    SkipSoundex = (Opt("ntech.trapets.kycinstantwatch.skipsoundex") ?? "false").ToLowerInvariant() == "true",
                    Endpoint = endpoint,
                    DebugLogFolder = Opt("ntech.trapets.kycinstantwatch.debuglogfolder")
                };
            }
        }

        public static DirectoryInfo SkinningRootFolder => E.ClientResourceDirectory("ntech.skinning.rootfolder", "Skinning", false);

        public static FileInfo SkinningCssFile => E.ClientResourceFile("ntech.skinning.cssfile", Path.Combine(SkinningRootFolder.FullName, "css\\skinning.css"), false);

        public static bool IsSkinningEnabled => NTechCache.WithCacheS($"ntech.cache.skinningenabled", TimeSpan.FromMinutes(5), () => NEnv.SkinningRootFolder?.Exists ?? false);

        public static bool IsSkinningCssEnabled => NTechCache.WithCacheS($"ntech.cache.skinningcssenabled", TimeSpan.FromMinutes(5), () => NEnv.SkinningCssFile?.Exists ?? false);

        public static string TestingOverrideDateFile
        {
            get
            {
                if (IsProduction)
                    return null;

                return E.StaticResourceFile("ntech.credit.testing.overridedatefile", "TestOverrideDate.txt", false).FullName;
            }
        }

        public static ICustomerEnvSettings EnvSettings => CustomerEnvSettings.Instance;

        public static IClientConfigurationCore ClientCfgCore =>
            NTechCache.WithCache("nCustomer.ClientCfgCore", TimeSpan.FromMinutes(15), () => ClientConfigurationCoreFactory.CreateUsingNTechEnvironment(E));

        public static bool IsTemplateCacheDisabled => string.Equals((Opt("ntech.document.disabletemplatecache") ?? "false"), "true", StringComparison.InvariantCultureIgnoreCase);

        private static NTechEnvironment E => NTechEnvironment.Instance;

        private static string Opt(string n) => E.Setting(n, false);

        private static string Req(string n) => E.Setting(n, true);
    }

    public class CustomerEnvSettings : ICustomerEnvSettings
    {
        private CustomerEnvSettings()
        {

        }

        public static ICustomerEnvSettings Instance { get; private set; } = new CustomerEnvSettings();

        public bool IsProduction => NEnv.IsProduction;

        public bool IsTemplateCacheDisabled => NEnv.IsTemplateCacheDisabled;

        public Dictionary<string, KycQuestionsTemplate> DefaultKycQuestionsSets => NEnv.DefaultKycQuestionsSets;
        public string RelativeKycLogFolder => NTechEnvironment.Instance.Setting("ntech.customer.kyc.queryitemslogfolder", false);
        public string LogFolder => NEnv.LogFolder.FullName;
    }
}