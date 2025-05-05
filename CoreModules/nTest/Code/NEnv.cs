using Duende.IdentityModel.Client;
using Newtonsoft.Json;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.BankAccounts.Se;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace nTest
{
    public static class NEnv
    {
        public static bool IsProduction => Req("ntech.isproduction") == "true";

        public static ClientConfiguration ClientCfg
        {
            get
            {
                return NTechCache.WithCache("nCredit.ClientCfg", TimeSpan.FromMinutes(15), () => ClientConfiguration.CreateUsingNTechEnvironment());
            }
        }

        public static FileInfo SeedPersonFile
        {
            get
            {
                var v = Opt("ntech.test.seedpersonfile");
                if (v == null)
                    return null;
                return new FileInfo(v);
            }
        }

        public static bool UseSqlServerDocumentDb => (Opt("ntech.ntest.usesqlserverdocumentdb") ?? "false").ToLowerInvariant() == "true";

        public static string DefaultProviderName => Req("ntech.ntest.defaultprovidername");

        public static DirectoryInfo AffiliatesFolder => E.ClientResourceDirectory("ntech.credit.affiliatesfolder", "Affiliates", true);

        public static IList<string> GetProviderNames(bool isMortgageLoanProvider)
        {
            return GetAffiliateModels().Where(x => x.IsMortgageLoanProvider == isMortgageLoanProvider).Select(x => x.ProviderName).ToList();
        }

        public static List<Affiliate> GetAffiliateModels()
        {
            return NTechCache.WithCache(
                "ntech.ntest.affilates",
                TimeSpan.FromMinutes(5),
                () => Directory
                    .GetFiles(AffiliatesFolder.FullName, "*.json")
                    .Select(x => JsonConvert.DeserializeObject<Affiliate>(File.ReadAllText(x)))
                    .ToList());
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
            public bool? UseLeads { get; set; }
        }

        public static NTechServiceRegistry ServiceRegistry
        {
            get
            {
                return NTechCache.WithCache(
                    "9daea977-6121-4f8c-8d31-801d6eceb0f80",
                    TimeSpan.FromMinutes(5),
                    () => NTechEnvironment.Instance.ServiceRegistry);
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

        private static Lazy<IBANToBICTranslator> iBANToBICTranslatorInstance = new Lazy<IBANToBICTranslator>(() => new IBANToBICTranslator());

        public static IBANToBICTranslator IBANToBICTranslatorInstance => iBANToBICTranslatorInstance.Value;

        public static string NTechCdnUrl => Opt("ntech.cdn.rooturl");

        public static bool IsMortgageLoansEnabled => ClientCfg.IsFeatureEnabled("ntech.feature.mortgageloans");
        public static bool IsStandardMortgageLoansEnabled => IsMortgageLoansEnabled && ClientCfg.IsFeatureEnabled("ntech.feature.mortgageloans.standard");

        public static bool IsUnsecuredLoansEnabled => ClientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans");
        public static bool IsUlStandardWebApplicationEnabled => ClientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans.webapplication");

        public static bool IsStandardUnsecuredLoansEnabled => IsUnsecuredLoansEnabled && ClientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans.standard");

        public static bool HasPreCredit => ClientCfg.IsFeatureEnabled("ntech.feature.precredit");

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

        public static bool IsSavingsEnabled => ServiceRegistry.ContainsService("nSavings");

        private static string Opt(string n)
        {
            return E.Setting(n, false);
        }

        private static string Req(string n)
        {
            return E.Setting(n, true);
        }

        private static NTechEnvironment E => NTechEnvironment.Instance;

        public static bool IsVerboseLoggingEnabled => (Opt("ntech.isverboseloggingenabled") ?? "false") == "true";

        public static DateTimeOffset DefaultTime => DateTimeOffset.ParseExact(Req("ntech.test.defaulttime"), "o", CultureInfo.InvariantCulture);

        public static Tuple<string, string> AutomationUsernameAndPassword => Tuple.Create(Req("ntech.automationuser.username"), Req("ntech.automationuser.password"));

        public static string AutomationBearerToken()
        {
            return NTechCache.WithCache("nTestAutomation.966ce525-6a5a-48b0-8b71-51217da90645", TimeSpan.FromMinutes(3), () =>
            {                
                var client = new HttpClient();
                var credentials = AutomationUsernameAndPassword;
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
                    throw new Exception("Bearer token login failed in nTest event automation :" + token.Result.Error);
                }

                return token.Result.AccessToken;
            });
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

        public static AutogiroSettingsModel AutogiroSettings
        {
            get
            {
                var f = NTechEnvironment.Instance.StaticResourceFile("ntech.test.autogirosettingsfile", "autogiro-settings.txt", true);

                var settings = NTechSimpleSettings.ParseSimpleSettingsFile(f.FullName);
                return new AutogiroSettingsModel
                {
                    HmacFileSealKey = settings.Opt("hmacfileseal.key"),
                    IsHmacFileSealEnabled = settings.OptBool("hmacfileseal.enabled"),
                    CustomerNr = settings.Req("customernr"),
                    OutgoingStatusFileExportProfileName = settings.Opt("exportprofile.outgoing.statusfile"),
                    BankGiroNr = BankGiroNumberSe.Parse(settings.Req("bankgironr")),
                    IncomingStatusFileImportFolder = settings.Req("import.statusfile.sourcefolder")
                };
            }
        }

        public static bool HasPerLoanDueDay => ClientCfg.IsFeatureEnabled("ntech.feature.perloandueday");

        public static string CurrentServiceName => "nTest";

        public class AutogiroSettingsModel
        {
            public bool IsHmacFileSealEnabled { get; set; }
            public string HmacFileSealKey { get; set; }
            public string CustomerNr { get; set; }
            public BankGiroNumberSe BankGiroNr { get; set; }
            public string OutgoingStatusFileExportProfileName { get; set; }
            public string IncomingStatusFileImportFolder { get; set; }
        }
    }
}