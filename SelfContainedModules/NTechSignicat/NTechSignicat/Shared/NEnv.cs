using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NTech.Services.Infrastructure
{
    public class NEnv : INEnv
    {
        private readonly IWebHostEnvironment hostingEnvironment;
        private readonly IConfiguration configuration;

        public NEnv(IWebHostEnvironment hostingEnvironment, IConfiguration configuration)
        {
            this.hostingEnvironment = hostingEnvironment;
            this.configuration = configuration;
        }

        public bool IsProduction
        {
            get
            {
                var p = RequiredSetting("ntech.isproduction");
                if (p == "true")
                    return true;
                else if (p == "false")
                    return false;
                else
                    throw new Exception("ntech.isproduction must be true or false exactly");
            }
        }

        public bool IsVerboseLoggingEnabled
        {
            get
            {
                return OptionalSetting("ntech.isverboseloggingenabled")?.ToLowerInvariant() == "true";
            }
        }

        public bool ForceLocalLogging
        {
            get
            {
                return OptionalSetting("ntech.forcelocallogging")?.ToLowerInvariant() == "true";
            }
        }

        public string OptionalSetting(string name)
        {
            return configuration.GetValue<string>(name, null);
        }

        public string RequiredSetting(string name)
        {
            var v = OptionalSetting(name);
            if (string.IsNullOrWhiteSpace(v))
                throw new Exception($"Missing appsetting {name}");
            return v;
        }

        private string StaticResourceFolder
        {
            get
            {
                return RequiredSetting("ntech.staticresourcefolder");
            }
        }

        private string ClientResourceFolder
        {
            get
            {
                return RequiredSetting("ntech.clientresourcefolder");
            }
        }

        public string StaticResource(string settingName, string resourceFolderRelativePath)
        {
            var v = OptionalSetting(settingName);
            if (v != null)
                return v;
            else
                return Path.Combine(StaticResourceFolder, resourceFolderRelativePath);
        }

        private string ClientResource(string settingName, string resourceFolderRelativePath)
        {
            var v = OptionalSetting(settingName);
            if (v != null)
                return v;
            else
                return Path.Combine(ClientResourceFolder, resourceFolderRelativePath);
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

        private NTechServiceRegistry serviceRegistry;

        public NTechServiceRegistry ServiceRegistry
        {
            get
            {
                if (serviceRegistry != null)
                    return serviceRegistry;

                var serviceRegistryFile = StaticResourceFile("ntech.serviceregistry", "serviceregistry.txt", true);
                var internalServiceRegistryFile = StaticResourceFile("ntech.serviceregistry.internal", "serviceregistry-internal.txt", false);
                serviceRegistry = NTechServiceRegistry.ParseFromFiles(serviceRegistryFile, internalServiceRegistryFile);

                return serviceRegistry;
            }
        }

        public string ServiceName => "NTechSignicat";

        public string SqliteDocumentDbFile => RequiredSetting("ntech.signicat.documentdb.sqlite.filename");

        public DirectoryInfo TempFolder
        {
            get
            {
                var f = OptionalSetting("ntech.tempfolder");
                if (f == null)
                    return null;
                return new DirectoryInfo(f);
            }
        }

        public DirectoryInfo LogFolder
        {
            get
            {
                var f = OptionalSetting("ntech.logfolder");
                if (f == null)
                    return null;
                return new DirectoryInfo(f);
            }
        }
    }

    public interface INEnv
    {
        bool IsProduction { get; }

        string OptionalSetting(string name);

        string RequiredSetting(string name);

        FileInfo StaticResourceFile(string settingName, string resourceFolderRelativePath, bool mustExist);

        FileInfo ClientResourceFile(string settingName, string resourceFolderRelativePath, bool mustExist);

        NTechServiceRegistry ServiceRegistry { get; }
        string ServiceName { get; }
        bool IsVerboseLoggingEnabled { get; }
        bool ForceLocalLogging { get; }
        string SqliteDocumentDbFile { get; }
        DirectoryInfo TempFolder { get; }
        DirectoryInfo LogFolder { get; }
    }
}