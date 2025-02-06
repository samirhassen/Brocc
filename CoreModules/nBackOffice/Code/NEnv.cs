using NTech.Services.Infrastructure;
using System;
using System.IO;

namespace nBackOffice
{
    public static class NEnv
    {
        public static bool IsProduction
        {
            get
            {
                return Req("ntech.isproduction") == "true";
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

        public static string ExtraCrossModuleNavigationTargetsFile
        {
            get
            {
                return Opt("ntech.crossmodulenavigation.extratargetsfile");
            }
        }

        public static DirectoryInfo SkinningRootFolder => NTechEnvironment.Instance.ClientResourceDirectory("ntech.skinning.rootfolder", "Skinning", false);

        public static FileInfo SkinningCssFile => NTechEnvironment.Instance.ClientResourceFile("ntech.skinning.cssfile", Path.Combine(SkinningRootFolder.FullName, "css\\skinning.css"), false);

        public static bool IsSkinningEnabled => NTechCache.WithCacheS($"ntech.cache.skinningenabled", TimeSpan.FromMinutes(5), () => NEnv.SkinningRootFolder?.Exists ?? false);

        public static bool IsSkinningCssEnabled => NTechCache.WithCacheS($"ntech.cache.skinningcssenabled", TimeSpan.FromMinutes(5), () => NEnv.SkinningCssFile?.Exists ?? false);
        
        public static ClientConfiguration ClientCfg
        {
            get
            {
                return NTechCache.WithCache("nBackOffice.ClientCfg", TimeSpan.FromMinutes(15), () => ClientConfiguration.CreateUsingNTechEnvironment());
            }
        }

        public static bool ShowNavMenuDebugInfo
        {
            get
            {
                return (Opt("ntech.backoffice.navmenu.showdebuginfo") ?? "false").ToLowerInvariant() == "true";
            }
        }

        public static bool IsMortgageLoansEnabled
        {
            get
            {
                return ClientCfg.IsFeatureEnabled("ntech.feature.mortgageloans");
            }
        }

        public static bool IsStandardMortgageLoansEnabled => IsMortgageLoansEnabled && ClientCfg.IsFeatureEnabled("ntech.feature.mortgageloans.standard");

        public static bool IsUnsecuredLoansEnabled
        {
            get
            {
                return ClientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans");
            }
        }

        public static bool IsStandardUnsecuredLoansEnabled => IsUnsecuredLoansEnabled && ClientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans.standard");

        public static NTechServiceRegistry ServiceRegistry
        {
            get
            {
                return NTechCache.WithCache(
                    "3f23e672-1154-453c-8883-41a5b046cef60",
                    TimeSpan.FromMinutes(5),
                    () => NTechEnvironment.Instance.ServiceRegistry);
            }
        }

        public static Tuple<string, string> AutomationUsernameAndPassword => Tuple.Create(Req("ntech.automationuser.username"), Req("ntech.automationuser.password"));

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

        public static bool IsBackUrlDisabled
        {
            get
            {
                //NOTE: Want this to be everywhere but easier to test with mortgage loans only so starting there
                return NEnv.IsMortgageLoansEnabled;
            }
        }

        public static bool IsDevelopingOnLocalHost
        {
            get
            {
                return !NEnv.IsProduction && Opt("ntech.development.islocalhost") == "true";
            }
        }

        /// <summary>
        /// Just to keep the old pages around for a while until we are sure the new embedded backoffice actually works properly
        /// </summary>
        public static bool AllowAccessToLegacyUserAdmin
        {
            get
            {
                return (Opt("ntech.useradmin.allowlegacy") ?? "false").ToLowerInvariant() == "true";
            }
        }

        public static string CurrentServiceName => "nBackOffice";
    }
}