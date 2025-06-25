using Microsoft.Extensions.Configuration;
using NTech.Core.Module.Shared.Infrastructure;

namespace NTech.Core.Module;

public class NEnv : INTechEnvironment
{
    private readonly Func<string, string> _getSetting;
    private readonly Func<string, string> _getConnectionString;

    public NEnv(IConfiguration configuration)
    {
        var machineSettingsFile = configuration.GetValue<string>("ntech.machinesettingsfile");
        if (machineSettingsFile != null)
        {
            var machineSettings =
                NTechSimpleSettingsCore.ParseAppSettingsFile(machineSettingsFile, forceFileExistance: true);
            _getSetting = x => machineSettings.Opt(x) ?? configuration.GetValue<string>(x, null);
        }
        else
            _getSetting = x => configuration.GetValue<string>(x, null);

        //Doing this so we dont have a local reference to configuration ... that in turn
        //to prevent issues with future method skipping over the machine setting merge.
        _getConnectionString = configuration.GetConnectionString;
    }

    public NEnv(Func<string, string> getSetting, Func<string, string> getConnectionString)
    {
        _getSetting = getSetting;
        _getConnectionString = getConnectionString;
    }

    public bool IsProduction
    {
        get
        {
            var p = RequiredSetting("ntech.isproduction").ToLowerInvariant();
            return p switch
            {
                "true" => true,
                "false" => false,
                _ => throw new Exception("ntech.isproduction must be true or false exactly")
            };
        }
    }

    public bool IsHttpRequestLoggingEnabled =>
        (OptionalSetting("ntech.host.apilogs.enable") ?? "").ToLowerInvariant() == "true";

    public bool IsVerboseLoggingEnabled =>
        OptionalSetting("ntech.isverboseloggingenabled")?.ToLowerInvariant() == "true";

    public string ClientConfigurationDocumentFile
    {
        get
        {
            var configFile = OptionalSetting("ntech.clientcfgfile");
            if (configFile != null) return configFile;

            var resourceDir = OptionalSetting("ntech.clientresourcefolder");
            if (resourceDir == null)
            {
                throw new Exception("Missing appsetting 'ntech.clientcfgfile'");
            }

            return Path.Combine(resourceDir, "ClientConfiguration.xml");
        }
    }

    public string OptionalSetting(string name)
    {
        return _getSetting(name);
    }

    public string RequiredSetting(string name)
    {
        var v = OptionalSetting(name);
        if (string.IsNullOrWhiteSpace(v))
            throw new Exception($"Missing appsetting {name}");
        return v;
    }

    public bool OptBoolSetting(string settingName) =>
        OptionalSetting(settingName ?? "false").Trim().ToLowerInvariant() == "true";

    private string StaticResourceFolder => RequiredSetting("ntech.staticresourcefolder");

    private string ClientResourceFolder => RequiredSetting("ntech.clientresourcefolder");

    public string StaticResource(string settingName, string resourceFolderRelativePath)
    {
        var v = OptionalSetting(settingName);
        return v ?? Path.Combine(StaticResourceFolder, resourceFolderRelativePath);
    }

    private string ClientResource(string settingName, string resourceFolderRelativePath)
    {
        var v = OptionalSetting(settingName);
        return v ?? Path.Combine(ClientResourceFolder, resourceFolderRelativePath);
    }

    public FileInfo StaticResourceFile(string settingName, string resourceFolderRelativePath, bool mustExist)
    {
        FileInfo result = null;
        var v = StaticResource(settingName, resourceFolderRelativePath);
        if (v != null)
        {
            result = new FileInfo(v);
        }

        if (result == null || (mustExist && !result.Exists))
        {
            throw new Exception(
                $"Missing static resource file. The static resource folder '{StaticResourceFolder}' needs to contain '{resourceFolderRelativePath}' or the appsetting '{settingName}' needs to point out where it can be found instead.");
        }

        return result;
    }

    public DirectoryInfo OptionalSharedResourcesDirectory =>
        ClientResourceDirectory("ntech.client.sharedresourcesfolder", "Shared", mustExist: false);

    public FileInfo ClientResourceFile(string settingName, string resourceFolderRelativePath, bool mustExist,
        bool useSharedFallback = false)
    {
        FileInfo result = null;
        var v = ClientResource(settingName, resourceFolderRelativePath);
        if (v != null)
        {
            result = new FileInfo(v);
        }

        if (useSharedFallback && (result == null || !result.Exists) && OptionalSharedResourcesDirectory != null)
        {
            result = new FileInfo(Path.Combine(OptionalSharedResourcesDirectory.FullName,
                resourceFolderRelativePath));
        }

        if (result == null || (mustExist && !result.Exists))
        {
            throw new Exception(
                $"Missing client resource file. The client resource folder '{ClientResourceFolder}' needs to contain '{resourceFolderRelativePath}' or the appsetting '{settingName}' needs to point out where it can be found instead.");
        }

        return result;
    }

    public DirectoryInfo ClientResourceDirectory(string settingName, string resourceFolderRelativePath,
        bool mustExist)
    {
        DirectoryInfo directoryInfo = null;
        var text = ClientResource(settingName, resourceFolderRelativePath);
        if (text != null)
        {
            directoryInfo = new DirectoryInfo(text);
        }

        if (directoryInfo == null && (mustExist && !directoryInfo.Exists))
        {
            throw new Exception("Missing client resource directory. The client resource folder '" +
                                ClientResourceFolder + "' needs to contain '" + resourceFolderRelativePath +
                                "' or the appsetting '" + settingName +
                                "' needs to point out where it can be found instead.");
        }

        return directoryInfo;
    }

    private NTechServiceRegistry serviceRegistry;

    public NTechServiceRegistry ServiceRegistry
    {
        get
        {
            if (serviceRegistry != null)
                return serviceRegistry;

            var serviceRegistryFile = StaticResourceFile("ntech.serviceregistry", "serviceregistry.txt", true);
            var internalServiceRegistryFile = StaticResourceFile("ntech.serviceregistry.internal",
                "serviceregistry-internal.txt", false);
            serviceRegistry = NTechServiceRegistry.ParseFromFiles(serviceRegistryFile, internalServiceRegistryFile);

            return serviceRegistry;
        }
    }

    public DirectoryInfo TempFolder
    {
        get
        {
            var f = OptionalSetting("ntech.tempfolder");
            return f == null ? null : new DirectoryInfo(f);
        }
    }

    public DirectoryInfo LogFolder
    {
        get
        {
            var f = OptionalSetting("ntech.logfolder");
            return f == null ? null : new DirectoryInfo(f);
        }
    }

    public string GetConnectionString(string name) => _getConnectionString(name);

    /// <summary>
    /// Set during startup to deal with some code paths that dont work properly with
    /// dependancy injection. Never use this if you have the option of injecting Nenv instead.
    /// </summary>
    public static NEnv SharedInstance { get; set; }
}