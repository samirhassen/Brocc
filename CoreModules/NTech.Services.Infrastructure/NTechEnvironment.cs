using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace NTech.Services.Infrastructure
{
    public class NTechEnvironment
    {
        private Func<string, string> getAppSetting;
        private Func<string, string> getMachineSetting;
        private Lazy<NTechSimpleSettings> machineSettings;
        private NTechServiceRegistry serviceRegistry = null;

        public NTechEnvironment(Func<string, string> getAppSetting = null, Func<string, string> getMachineSetting = null)
        {
            this.getAppSetting = getAppSetting ?? (n => WebConfigurationManager.AppSettings[n]);
            this.machineSettings = new Lazy<NTechSimpleSettings>(() =>
            {
                var f = (this.getAppSetting("ntech.machinesettingsfile") ?? "").Trim();
                if (string.IsNullOrWhiteSpace(f))
                    return null;
                return NTechSimpleSettings.ParseAppSettingsFile(f, true);
            });
            this.getMachineSetting = getMachineSetting ?? (x => this.machineSettings.Value?.Opt(x));
        }

        private static Lazy<NTechEnvironment> instance = new Lazy<NTechEnvironment>(() => new NTechEnvironment());

        public static NTechEnvironment Instance
        {
            get
            {
                return instance.Value;
            }
        }

        public bool OptBoolSetting(string settingName, bool skipSettingsTokens = false)
        {
            return (Setting(settingName, false, skipSettingsTokens: skipSettingsTokens) ?? "false").Trim().ToLowerInvariant() == "true";
        }

        ///<summary>
        /// Allows using a feature toggle to control something in the application
        /// while also allowing local override in specific environment using an appsetting
        ///</summary>
        public bool IsFeatureEnabledWithAppSettingOverride(string settingName, ClientConfiguration clientConfiguration, bool skipSettingsTokens = false)
        {
            var appSetting = Setting(settingName, false, skipSettingsTokens: skipSettingsTokens);
            if(appSetting == null)
                return clientConfiguration.ActiveFeatures.Contains(settingName);
            return appSetting.Trim().ToLowerInvariant() == "true";
        }

        public string Setting(string settingName, bool mustExist, bool skipSettingsTokens = false)
        {
            var v = (this.getAppSetting(settingName) ?? "").Trim();
            if (string.IsNullOrWhiteSpace(v))
                v = this.getMachineSetting(settingName);

            if (string.IsNullOrWhiteSpace(v))
            {
                if (mustExist)
                    throw new Exception($"Missing required appsetting: '{settingName}'");
                else
                    return null;
            }

            return skipSettingsTokens ? v : ReplaceSettingsTokens(v);
        }

        private string ReplaceSettingsTokens(string v)
        {
            if (string.IsNullOrWhiteSpace(v))
                return v;
            return Regex.Replace(v, @"\{(iurl|eurl)\:([^\}]+)\}", x =>
            {
                var t = x.Groups[1].Value?.ToLowerInvariant(); ;
                var serviceName = x.Groups[2].Value;
                if (ServiceRegistry.ContainsService(serviceName))
                    return (t == "iurl" ? ServiceRegistry.Internal : serviceRegistry.External).ServiceRootUri(serviceName).ToString().TrimEnd('/');
                else
                    return x.Value;
            }, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        }

        public FileInfo ClientResourceFile(string settingName, string resourceFolderRelativePath, bool mustExist)
        {
            FileInfo result = null;
            var v = ClientResource(settingName, resourceFolderRelativePath);
            if (v != null)
            {
                result = new FileInfo(v);
            }
            if (result == null || (mustExist && !result.Exists))
            {
                throw new Exception($"Missing client resource file. The client resource folder '{ClientResourceFolder}' needs to contain '{resourceFolderRelativePath}' or the appsetting '{settingName}' needs to point out where it can be found instead.");
            }
            return result;
        }

        public DirectoryInfo ClientResourceDirectory(string settingName, string resourceFolderRelativePath, bool mustExist)
        {
            DirectoryInfo result = null;
            var v = ClientResource(settingName, resourceFolderRelativePath);
            if (v != null)
            {
                result = new DirectoryInfo(v);
            }
            if (result == null || (mustExist && !result.Exists))
            {
                throw new Exception($"Missing client resource directory. The client resource folder '{ClientResourceFolder}' needs to contain '{resourceFolderRelativePath}' or the appsetting '{settingName}' needs to point out where it can be found instead.");
            }
            return result;
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
                throw new Exception($"Missing static resource file. The static resource folder '{StaticResourceFolder}' needs to contain '{resourceFolderRelativePath}' or the appsetting '{settingName}' needs to point out where it can be found instead.");
            }
            return result;
        }

        public DirectoryInfo StaticResourceDirectory(string settingName, string resourceFolderRelativePath, bool mustExist)
        {
            DirectoryInfo result = null;
            var v = StaticResource(settingName, resourceFolderRelativePath);
            if (v != null)
            {
                result = new DirectoryInfo(v);
            }
            if (result == null || (mustExist && !result.Exists))
            {
                throw new Exception($"Missing static resource directory. The static resource folder '{StaticResourceFolder}' needs to contain '{resourceFolderRelativePath}' or the appsetting '{settingName}' needs to point out where it can be found instead.");
            }
            return result;
        }

        public DirectoryInfo SharedResourceDirectory
        {
            get 
            {
                return ClientResourceDirectory("ntech.client.sharedresourcesfolder", $"Shared", true);
            }
        }

        public void ReloadServiceRegistry()
        {
            const string filePrefix = "file:";
            var v = Setting("ntech.serviceregistry", false);
            FileInfo serviceRegistryFile;
            if (!string.IsNullOrWhiteSpace(v) && v.StartsWith(filePrefix))
            {
                //Older syntax
                serviceRegistryFile = new FileInfo(v.Substring(filePrefix.Length));
                if (!serviceRegistryFile.Exists)
                    throw new Exception($"Missing service registry file: {serviceRegistryFile.FullName}");
            }
            else
            {
                serviceRegistryFile = StaticResourceFile("ntech.serviceregistry", "serviceregistry.txt", true);
            }
            var internalServiceRegistryFile = StaticResourceFile("ntech.serviceregistry.internal", "serviceregistry-internal.txt", false);
            serviceRegistry = NTechServiceRegistry.ParseFromFiles(serviceRegistryFile, internalServiceRegistryFile);
        }

        public NTechServiceRegistry ServiceRegistry
        {
            get
            {
                if (serviceRegistry == null)
                {
                    ReloadServiceRegistry();
                }
                return serviceRegistry;
            }
        }

        public bool IsProduction
        {
            get
            {
                var s = Setting("ntech.isproduction", true, skipSettingsTokens: true) ?? "";
                return s.Trim().ToLower() == "true";
            }
        }

        private string ClientResource(string settingName, string resourceFolderRelativePath)
        {
            var v = Setting(settingName, false);
            if (v != null)
                return v;
            else
                return Path.Combine(ClientResourceFolder, resourceFolderRelativePath);
        }

        private string StaticResource(string settingName, string resourceFolderRelativePath)
        {
            var v = Setting(settingName, false);
            if (v != null)
                return v;
            else
                return Path.Combine(StaticResourceFolder, resourceFolderRelativePath);
        }

        /// <summary>
        /// Settings that are the same in test and production but differ per client
        /// </summary>
        private string ClientResourceFolder
        {
            get
            {
                return Setting("ntech.clientresourcefolder", true);
            }
        }

        /// <summary>
        /// Settings that differ per machine/environment
        /// </summary>
        private string StaticResourceFolder
        {
            get
            {
                return Setting("ntech.staticresourcefolder", true);
            }
        }
    }
}