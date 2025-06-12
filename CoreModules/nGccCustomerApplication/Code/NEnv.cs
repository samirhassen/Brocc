using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;
using NTech.Services.Infrastructure;

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

        public static bool RedirectOldApplication =>
            (Opt("ntech.gcccustomerapplication.redirectoldapplication") ?? "false") == "true";

        public static XDocument KycQuestions
        {
            get
            {
                return NTechCache.WithCache(
                    "9ac532a7-c38e-45c7-83ac-34676d9ebb9d",
                    TimeSpan.FromHours(4),
                    () => XDocuments.Load(
                        NTechEnvironment.Instance
                            .ClientResourceFile("ntech.credit.kycquestionsfile", "KycQuestions.xml", true).FullName));
            }
        }

        public static Tuple<string, string> CreateApplicationCredentials
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Opt("ntech.gcccustomerapplication.precreditcredentials.username")))
                    return Tuple.Create(Req("ntech.gcccustomerapplication.precreditcredentials.username"),
                        Req("ntech.gcccustomerapplication.precreditcredentials.password"));

                return Tuple.Create(Req("ntech.automationuser.username"), Req("ntech.automationuser.password"));
            }
        }

        public static Tuple<string, string> SystemUserCredentials => Tuple.Create(Req("ntech.automationuser.username"),
            Req("ntech.automationuser.password"));

        public static string GetSelfCachingSystemUserBearerToken()
        {
            return NTechCache.WithCache("9ac532c38e-45cads7-83ac-34676d9ebbdasd9d", TimeSpan.FromMinutes(5), () =>
            {
                var u = CreateApplicationCredentials;
                return NHttp.AquireSystemUserAccessTokenWithUsernamePassword(u.Item1, u.Item2,
                    ServiceRegistry.Internal.ServiceRootUri("nUser"));
            });
        }

        public static string SelfProviderName => Req("ntech.gcccustomerapplication.providername");

        public static string EoneProviderName => Req("ntech.gcccustomerapplication.eone.providername");

        public static string EtuaProviderName => Req("ntech.gcccustomerapplication.etua.providername");

        public static string SalusProviderName => Req("ntech.gcccustomerapplication.salus.providername");

        public static bool IsVerboseLoggingEnabled => (Opt("ntech.isverboseloggingenabled") ?? "false") == "true";

        public static bool IsCachingEnabled =>
            (Opt("ntech.gcccustomerapplication.disablecaching") ?? "false").ToLowerInvariant() == "true";

        public static Uri SuccessUrl
        {
            get
            {
                var s = Opt("ntech.gcccustomerapplication.successurl");
                return s == null ? null : new Uri(s);
            }
        }

        public static Uri FailedUrl
        {
            get
            {
                var s = Opt("ntech.gcccustomerapplication.failedurl");
                return s == null ? null : new Uri(s);
            }
        }

        public static ISet<string> ProviderNamesWithDisabledAutomation
        {
            get
            {
                var s = Opt("ntech.gcccustomerapplication.providerswithdisabledautomation");
                return s == null
                    ? new HashSet<string>()
                    : new HashSet<string>(s.Split(',').Where(x => !string.IsNullOrWhiteSpace(x)));
            }
        }

        public static DirectoryInfo AffiliatesFolder =>
            NTechEnvironment.Instance.ClientResourceDirectory("ntech.credit.affiliatesfolder", "Affiliates",
                true);

        public static Affiliate GetAffiliateModel(string providerName, bool allowMissing = false)
        {
            return NTechCache.WithCache($"ntech.credit.affilate.{providerName}", TimeSpan.FromMinutes(15), () =>
            {
                var path = AffiliatesFolder;
                var affilateFile = Path.Combine(path.FullName, providerName + ".json");

                if (File.Exists(affilateFile))
                    return JsonConvert.DeserializeObject<Affiliate>(File.ReadAllText(affilateFile));

                if (allowMissing)
                    return null;
                throw new Exception("Missing affiliate file: " + affilateFile);
            });
        }

        public static ClientConfiguration ClientCfg
        {
            get
            {
                return NTechCache.WithCache("nGccCustomerApplication.ClientCfg", TimeSpan.FromMinutes(15),
                    () => ClientConfiguration.CreateUsingNTechEnvironment());
            }
        }

        public static decimal? LegalInterestCeilingPercent
        {
            get
            {
                var c = Opt("ntech.legalinterestceilingpercent") ??
                        ClientCfg.OptionalSetting("ntech.legalinterestceilingpercent");
                return c != null ? (decimal?)decimal.Parse(c, CultureInfo.InvariantCulture) : null;
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

        public static string ProviderApplicationLogFolder =>
            Opt("ntech.gcccustomerapplication.providerapplicationlogfolder");

        public static NTechServiceRegistry ServiceRegistry => NTechEnvironment.Instance.ServiceRegistry;

        public static string CurrentServiceName => "nGccCustomerApplication";
    }
}