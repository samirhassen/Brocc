using System;
using System.IO;
using NTech.Core.Module.Shared;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;

namespace nDocument
{
    public static class NEnv
    {
        public static bool IsProduction => Req("ntech.isproduction") == "true";

        public static string PrinceXmlExePath => Req("ntech.princexml.exepath");

        public static string WeasyPrintExePath => Opt("ntech.weasyprint.exepath");

        public static string PrinceXmlPdfProfile => Opt("ntech.princexml.pdfprofile");

        public static string StaticHtmlToPdfProviderName => Opt("ntech.htmltopdf.providername") ?? "princexml";

        public static Uri StaticHtmlToPdfServiceUrl =>
            //TODO: Make this a standard integrated service and move it to the serviceregistry.
            new Uri(Req("ntech.htmltopdf.chromeheadless.url"));

        public static ClientConfiguration ClientCfg
        {
            get
            {
                return NTechCache.WithCache("nDocument.ClientCfg", TimeSpan.FromMinutes(15),
                    () => ClientConfiguration.CreateUsingNTechEnvironment());
            }
        }

        public static string FileExportProfilesFile =>
            NTechEnvironment.Instance
                .StaticResourceFile("ntech.archive.fileexport.profiles", "FileExportProfiles.xml", true).FullName;

        public static DirectoryInfo DocumentCreationRequestLogFolder
        {
            get
            {
                var v = Opt("ntech.documentcreation.requestlogfolder");
                return v == null ? null : new DirectoryInfo(v);
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

        public static PrinceXmlLicense PrinceLicense
        {
            get
            {
                var s = new PrinceXmlLicense
                {
                    FileName = Opt("ntech.princexml.licensefilepath"),
                    Key = Opt("ntech.princexml.licensekey")
                };
                if (IsProduction && string.IsNullOrWhiteSpace(s.FileName) && string.IsNullOrWhiteSpace(s.Key))
                {
                    throw new Exception(
                        "Require at least one of the settings 'ntech.princexml.licensefilepath' and 'ntech.princexml.licensekey'");
                }

                return s;
            }
        }

        public class PrinceXmlLicense
        {
            public string FileName { get; set; }
            public string Key { get; set; }
        }

        public static bool IsVerboseLoggingEnabled => (Opt("ntech.isverboseloggingenabled") ?? "false") == "true";

        public static Tuple<string, string> AzureStorageContainernameAndConnectionstring =>
            Tuple.Create(
                Req("ntech.archive.storageprovider.azure.containername"),
                Req("ntech.archive.storageprovider.azure.connectionstring"));

        public enum StorageProviderCode
        {
            Azure,
            Aws,
            Disk,
            Sqlite
        }

        public static StorageProviderCode StorageProvider
        {
            get
            {
                var provider = Req("ntech.archive.storageprovider.name")?.ToLowerInvariant();
                switch (provider)
                {
                    case "disk":
                        return StorageProviderCode.Disk;
                    case "azure":
                        return StorageProviderCode.Azure;
                    case "aws":
                        return StorageProviderCode.Aws;
                    case "sqlite":
                        return StorageProviderCode.Sqlite;
                    default:
                        throw new Exception(
                            "Invalid value for appsetting ntech.archive.storageprovider.name. Valid values are disk|azure|aws");
                }
            }
        }

        public static StorageProviderCode BackupStorageProvider
        {
            get
            {
                var provider = Req("ntech.archive.backupstorageprovider.name")?.ToLowerInvariant();
                switch (provider)
                {
                    case "disk":
                        return StorageProviderCode.Disk;
                    case "azure":
                        return StorageProviderCode.Azure;
                    case "aws":
                        return StorageProviderCode.Aws;
                    case "sqlite":
                        return StorageProviderCode.Sqlite;
                    default:
                        throw new Exception(
                            "Invalid value for appsetting ntech.archive.backupstorageprovider.name. Valid values are disk|azure|aws");
                }
            }
        }

        public static DirectoryInfo DiskStorageProviderRootFolder =>
            new DirectoryInfo(Req("ntech.archive.storageprovider.disk.rootfolder"));

        public static FileInfo SqliteStorageProviderDatabaseFile =>
            new FileInfo(Req("ntech.archive.storageprovider.sqlite.file"));

        public static NTechServiceRegistry ServiceRegistry
        {
            get
            {
                return NTechCache.WithCache(
                    "7196fba7-4676-4b26-8e04-255f8dc9e8010",
                    TimeSpan.FromMinutes(5),
                    () => NTechEnvironment.Instance.ServiceRegistry);
            }
        }

        public static bool IsHtmlTemplateLoggingEnabled =>
            (Opt("ntech.documentcreation.ishtmltemplateloggingenabled") ?? "false").Trim().ToLower() ==
            "true";

        public static bool IsHardeningEnabled => ClientCfg.IsFeatureEnabled("ntech.harden.document");

        public static bool IsBackupProviderSet => Opt("ntech.archive.backupstorageprovider.isset") == "true";

        public static string CurrentServiceName => "nDocument";

        public static bool IsTemplateCacheDisabled => string.Equals(
            Opt("ntech.document.disabletemplatecache") ?? "false", "true",
            StringComparison.InvariantCultureIgnoreCase);

        public static Lazy<NTechSelfRefreshingBearerToken> AutomationUserBearerToken =>
            new Lazy<NTechSelfRefreshingBearerToken>(() =>
                NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(
                    ServiceRegistry,
                    Req("ntech.automationuser.username"),
                    Req("ntech.automationuser.password")));

        public static IClientConfigurationCore ClientCfgCore =>
            NTechCache.WithCache("nDocument.ClientCfgCore", TimeSpan.FromMinutes(15),
                () => ClientConfigurationCoreFactory.CreateUsingNTechEnvironment(NTechEnvironment.Instance));

        public static ISharedEnvSettings SharedEnv => DocumentEnvSettings.Instance;

        private static string Opt(string n)
        {
            return NTechEnvironment.Instance.Setting(n, false);
        }

        private static string Req(string n)
        {
            return NTechEnvironment.Instance.Setting(n, true);
        }
    }

    public class DocumentEnvSettings : ISharedEnvSettings
    {
        private DocumentEnvSettings()
        {
        }

        public static ISharedEnvSettings Instance { get; private set; } = new DocumentEnvSettings();

        public bool IsProduction => NEnv.IsProduction;

        public bool IsTemplateCacheDisabled => NEnv.IsTemplateCacheDisabled;
    }
}