using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System;
using System.IO;
using System.Web.Configuration;

namespace NTech.Legacy.Module.Shared
{    
    public class NTechEnvironmentLegacy : INTechEnvironment
    {
        public static INTechEnvironment SharedInstance { get; } = new NTechEnvironmentLegacy();
        public bool IsProduction => NTechEnvironment.Instance.IsProduction;
        public DirectoryInfo ClientResourceDirectory(string settingName, string resourceFolderRelativePath, bool mustExist) =>
            NTechEnvironment.Instance.ClientResourceDirectory(settingName, resourceFolderRelativePath, mustExist);
        public FileInfo ClientResourceFile(string settingName, string resourceFolderRelativePath, bool mustExist, bool useSharedFallback = false)
        {
            if (useSharedFallback)
                throw new NotImplementedException("Legacy does not implement the shared fallback");
            return NTechEnvironment.Instance.ClientResourceFile(settingName, resourceFolderRelativePath, mustExist);
        }
        public string GetConnectionString(string name) => WebConfigurationManager.ConnectionStrings[name].ConnectionString;
        public string OptionalSetting(string name) => NTechEnvironment.Instance.Setting(name, false);
        public string RequiredSetting(string name) => NTechEnvironment.Instance.Setting(name, true);
        public bool OptBoolSetting(string settingName) => NTechEnvironment.Instance.OptBoolSetting(settingName);
        public FileInfo StaticResourceFile(string settingName, string resourceFolderRelativePath, bool mustExist) =>
            NTechEnvironment.Instance.StaticResourceFile(settingName, resourceFolderRelativePath, mustExist);
    }
}
