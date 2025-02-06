using System.IO;

namespace NTech.Core.Module.Shared.Infrastructure
{
    public interface INTechEnvironment
    {
        bool IsProduction { get; }
        string OptionalSetting(string name);
        string RequiredSetting(string name);
        bool OptBoolSetting(string name);
        FileInfo StaticResourceFile(string settingName, string resourceFolderRelativePath, bool mustExist);
        FileInfo ClientResourceFile(string settingName, string resourceFolderRelativePath, bool mustExist, bool useSharedFallback = false);
        DirectoryInfo ClientResourceDirectory(string settingName, string resourceFolderRelativePath, bool mustExist);
        string GetConnectionString(string name);
    }
}
