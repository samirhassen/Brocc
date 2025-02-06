using nCredit;
using nCredit.DbModel.DomainModel;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;

namespace NTech.Core.Credit.Services
{
    public class NotificationProcessSettingsFactoryCore : NotificationProcessSettingsFactoryBase
    {
        private readonly NEnv environment;

        public NotificationProcessSettingsFactoryCore(IClientConfigurationCore clientConfiguration, CachedSettingsService cachedSettingsService, ICreditEnvSettings envSettings,
            NEnv environment) : base(clientConfiguration, cachedSettingsService, envSettings)
        {
            this.environment = environment;
        }

        protected override string GetAppSetting(string name, bool mustExist) =>
            mustExist ? environment.RequiredSetting(name) : environment.OptionalSetting(name);

        protected override FileInfo GetClientResourceFile(string settingName, string resourceFolderRelativePath, bool mustExist) =>
            environment.ClientResourceFile(settingName, resourceFolderRelativePath, mustExist);
    }
}