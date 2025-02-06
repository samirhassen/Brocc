using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace nDataWarehouse
{
    public static class NEnv
    {
        public static string CurrentServiceName
        {
            get
            {
                return "nDataWarehouse";
            }
        }

        public static bool IsProduction
        {
            get
            {
                return Req("ntech.isproduction") == "true";
            }
        }

        public static ClientConfiguration ClientCfg
        {
            get
            {
                return NTechCache.WithCache("nDataWarehouse.ClientCfg", TimeSpan.FromMinutes(15), () => ClientConfiguration.CreateUsingNTechEnvironment());
            }
        }

        public static NTechServiceRegistry ServiceRegistry
        {
            get
            {
                return NTechCache.WithCache(
                    "0dcd1b94-f71a-4932-b8a9-8aa0a9a78bb30",
                    TimeSpan.FromMinutes(5),
                    () => NTechEnvironment.Instance.ServiceRegistry);
            }
        }

        public static bool IsDashboardEnabled
        {
            get
            {
                return (ClientCfg.OptionalSetting("ntech.dashboard.enabled") ?? "false").ToLowerInvariant().Trim() == "true";
            }
        }

        public static bool IsCompanyLoansEnabled
        {
            get
            {
                ThrowIfMultipleLoansTypesEnabled();
                return ClientCfg.IsFeatureEnabled("ntech.feature.companyloans");
            }
        }

        public static bool IsMortgageLoansEnabled
        {
            get
            {
                ThrowIfMultipleLoansTypesEnabled();
                return ClientCfg.IsFeatureEnabled("ntech.feature.mortgageloans");
            }
        }

        public static bool IsUnsecuredLoansEnabled
        {
            get
            {
                ThrowIfMultipleLoansTypesEnabled();
                return ClientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans");
            }
        }

        public static int? TreatNotificationsAsClosedMaxBalance
        {
            get
            {
                var s = ClientCfg.OptionalSetting("ntech.credit.notification.lowbalancelimit");
                if (string.IsNullOrWhiteSpace(s))
                    return null;
                else
                    return int.Parse(s.Trim());
            }
        }

        public static void ThrowIfMultipleLoansTypesEnabled()
        {
            var i = 0;
            if (ClientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans")) i++;
            if (ClientCfg.IsFeatureEnabled("ntech.feature.mortgageloans")) i++;
            if (ClientCfg.IsFeatureEnabled("ntech.feature.companyloans")) i++;

            if (i > 1)
                throw new Exception("Multiple loantypes enabled but not supported!");
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

        public static bool IsBundlingEnabled
        {
            get
            {
                var s = Opt("ntech.isbundlingenabled") ?? "true";
                return s.Trim().ToLower() == "true";
            }
        }

        public static string NTechCdnUrl
        {
            get
            {
                return Opt("ntech.cdn.rooturl");
            }
        }

        public static XDocument DatawarehouseModel
        {
            get
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName;
                if (IsUnsecuredLoansEnabled || IsCompanyLoansEnabled)
                    resourceName = "nDataWarehouse.Resources.Common-DatawarehouseModel.xml";
                else if (IsMortgageLoansEnabled)
                    resourceName = "nDataWarehouse.Resources.MortgageLoan-DatawarehouseModel.xml";
                else
                    throw new NotImplementedException();

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    var d = XDocuments.Load(stream);
                    var c = ClientCfg;
                    foreach (var e in d.Descendants().Where(x => x.HasAttributes && x.Attribute("featureToggle") != null).ToList())
                    {
                        var isEnabled = e.Attribute("featureToggle").Value.Split('|').Any(c.IsFeatureEnabled);
                        if (!isEnabled)
                            e.Remove();
                    }

                    return d;
                }
            }
        }

        public static Tuple<string, string> AutomationUsernameAndPassword
        {
            get
            {
                var un = Opt("ntech.automationuser.username");
                var pw = Opt("ntech.automationuser.password");
                return (string.IsNullOrWhiteSpace(un) || string.IsNullOrWhiteSpace(pw))
                    ? null :
                    Tuple.Create(un, pw);
            }
        }

        public static DirectoryInfo SkinningRootFolder => NTechEnvironment.Instance.ClientResourceDirectory("ntech.skinning.rootfolder", "Skinning", false);

        public static FileInfo SkinningCssFile => NTechEnvironment.Instance.ClientResourceFile("ntech.skinning.cssfile", Path.Combine(SkinningRootFolder.FullName, "css\\skinning.css"), false);

        public static bool IsSkinningEnabled => NTechCache.WithCacheS($"ntech.cache.skinningenabled", TimeSpan.FromMinutes(5), () => NEnv.SkinningRootFolder?.Exists ?? false);

        public static bool IsSkinningCssEnabled => NTechCache.WithCacheS($"ntech.cache.skinningcssenabled", TimeSpan.FromMinutes(5), () => NEnv.SkinningCssFile?.Exists ?? false);

        private static string Opt(string n)
        {
            return NTechEnvironment.Instance.Setting(n, false);
        }

        private static string Req(string n)
        {
            return NTechEnvironment.Instance.Setting(n, true);
        }

        public static bool IsVerboseLoggingEnabled
        {
            get
            {
                return (Opt("ntech.isverboseloggingenabled") ?? "false") == "true";
            }
        }

        public class CivicRegNrInsertionSettingsModel
        {
            public bool IsEnabled { get; set; }
            public string SystemUserUsername { get; set; }
            public string SystemUserPassword { get; set; }

            public string GetSystemUserBearerToken()
            {
                return NTechCache.WithCache("b0a24abd-0da8-452e-b229-c823fe1b2210", TimeSpan.FromMinutes(3), () =>
                    NHttp.AquireSystemUserAccessTokenWithUsernamePassword(SystemUserUsername, SystemUserPassword, ServiceRegistry.Internal.ServiceRootUri("nUser")));
            }
        }

        public static CivicRegNrInsertionSettingsModel CivicRegNrInsertionSettings
        {
            get
            {
                var f = NTechEnvironment.Instance.StaticResourceFile("ndatawarehouse.civicregnrinsertion.settingsfile", "ndatawarehouse-civicregnrinsertion-settings.txt", false);
                var s = NTechSimpleSettings.ParseSimpleSettingsFile(f.FullName);
                if (!f.Exists)
                    return new CivicRegNrInsertionSettingsModel
                    {
                        IsEnabled = false
                    };
                else
                    return new CivicRegNrInsertionSettingsModel
                    {
                        IsEnabled = s.OptBool("enabled"),
                        SystemUserUsername = s.Req("systemuser.username"),
                        SystemUserPassword = s.Req("systemuser.password"),
                    };
            }
        }

        public static DirectoryInfo AffiliatesFolder => NTechEnvironment.Instance.ClientResourceDirectory("ntech.credit.affiliatesfolder", "Affiliates", true);

        public static List<ProviderDisplayNames.AffiliateModelPartial> GetAffiliateModels()
        {
            return NTechCache.WithCache(
                "ntech.dw.affilates",
                TimeSpan.FromMinutes(5),
                () => Directory
                    .GetFiles(AffiliatesFolder.FullName, "*.json")
                    .Select(x => JsonConvert.DeserializeObject<ProviderDisplayNames.AffiliateModelPartial>(File.ReadAllText(x)))
                    .ToList());
        }
    }
}