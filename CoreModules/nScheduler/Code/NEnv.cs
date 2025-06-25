using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using nScheduler.Code;
using NTech.Services.Infrastructure;

namespace nScheduler
{
    public static class NEnv
    {
        public static bool IsProduction => Req("ntech.isproduction") == "true";

        public static bool IsTelemetryLoggingEnabled =>
            (Opt("ntech.scheduler.telemetrylogging.enabled") ?? "true").Trim().ToLowerInvariant() == "true";

        public static DirectoryInfo LogFolder
        {
            get
            {
                var v = Opt("ntech.logfolder");
                return v == null ? null : new DirectoryInfo(v);
            }
        }

        public static ClientConfiguration ClientCfg
        {
            get
            {
                return NTechCache.WithCache("nScheduler.ClientCfg", TimeSpan.FromMinutes(15),
                    () => ClientConfiguration.CreateUsingNTechEnvironment());
            }
        }

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
                return NTechCache.WithCache("nScheduler.EncryptionKeys", TimeSpan.FromMinutes(15), () => NHttp
                    .Begin(ServiceRegistryNormal.Internal.ServiceRootUri("nUser"),
                        NHttp.GetCurrentAccessToken())
                    .PostJson("Encryption/KeySet", new { })
                    .ParseJsonAs<EncryptionKeySet>());
            }
        }

        public static NTechServiceRegistry ServiceRegistryNormal
        {
            get
            {
                return NTechCache.WithCache(
                    "138819bd-fe35-4b25-b104-801150e2dcf601",
                    TimeSpan.FromMinutes(5),
                    () => NTechEnvironment.Instance.ServiceRegistry);
            }
        }

        public static ServiceRegistry ServiceRegistryScheduler
        {
            get
            {
                return NTechCache.WithCache(
                    "138819bd-fe35-4b25-b104-801150e2dcf602",
                    TimeSpan.FromMinutes(5),
                    () => ServiceRegistry.CreateFromDict(NTechEnvironment.Instance.ServiceRegistry.Internal));
            }
        }

        public static SchedulerModel SchedulerModel
        {
            get
            {
                return NTechCache.WithCache("ntech.scheduler.model", TimeSpan.FromMinutes(15), () =>
                {
                    SchedulerModel sharedJobs = null;
                    var sharedResourcesDir = OptionalSharedResourcesDirectory;
                    if (sharedResourcesDir != null)
                    {
                        var sharedJobsFile =
                            XDocument.Load(Path.Combine(Path.Combine(sharedResourcesDir.FullName, "ScheduledJobs"),
                                "ScheduledJobs.xml"));
                        sharedJobs = SchedulerModel.Parse(sharedJobsFile, ServiceRegistryScheduler,
                            ClientCfg.IsFeatureEnabled, ClientCfg.Country.BaseCountry);
                    }

                    var file = NTechEnvironment.Instance.ClientResourceFile("ntech.scheduler.modelfile",
                        "ScheduledJobs.xml", true);
                    var doc = XDocuments.Load(file.FullName);

                    var clientJobs = SchedulerModel.Parse(doc, ServiceRegistryScheduler, ClientCfg.IsFeatureEnabled,
                        ClientCfg.Country.BaseCountry);

                    SchedulerModelCombinator.MergeSharedJobsIntoClientJobs(sharedJobs, clientJobs);
                    return clientJobs;
                });
            }
        }

        private static DirectoryInfo OptionalSharedResourcesDirectory
        {
            get
            {
                try
                {
                    //This is missing an optional version so we are reconstructing one here. Some clients dont have shared resources.
                    return NTechEnvironment.Instance.SharedResourceDirectory;
                }
                catch
                {
                    return null;
                }
            }
        }

        public static (string Username, string Password) AutomationUser =>
            (Username: Req("ntech.automationuser.username"), Password: Req("ntech.automationuser.password"));

        public static string TestingOverrideDateFile
        {
            get
            {
                if (IsProduction)
                    return null;
                return Opt("ntech.credit.testing.overridedatefile");
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

        public static string SchedulerAlertProvider => Opt("ntech.scheduler.alertprovider") ?? "none";

        public static List<string> SchedulerAlertEmail => Req("ntech.scheduler.alertemail").Split(';').ToList();

        public static DirectoryInfo SkinningRootFolder =>
            NTechEnvironment.Instance.ClientResourceDirectory("ntech.skinning.rootfolder", "Skinning", false);

        public static FileInfo SkinningCssFile => NTechEnvironment.Instance.ClientResourceFile("ntech.skinning.cssfile",
            Path.Combine(SkinningRootFolder.FullName, "css\\skinning.css"), false);

        public static bool IsSkinningEnabled => NTechCache.WithCacheS($"ntech.cache.skinningenabled",
            TimeSpan.FromMinutes(5), () => SkinningRootFolder?.Exists ?? false);

        public static bool IsSkinningCssEnabled => NTechCache.WithCacheS($"ntech.cache.skinningcssenabled",
            TimeSpan.FromMinutes(5), () => SkinningCssFile?.Exists ?? false);

        private static string Opt(string n)
        {
            return NTechEnvironment.Instance.Setting(n, false);
        }

        private static string Req(string n)
        {
            return NTechEnvironment.Instance.Setting(n, true);
        }

        public static bool IsVerboseLoggingEnabled => (Opt("ntech.isverboseloggingenabled") ?? "false") == "true";

        public static string CurrentServiceName => "nScheduler";
    }
}