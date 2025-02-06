using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Services.Infrastructure;
using System.IO;


namespace nCredit.DbModel.DomainModel
{
    public class NotificationProcessSettingsFactory2 : NotificationProcessSettingsFactoryBase
    {
        public NotificationProcessSettingsFactory2(IClientConfigurationCore clientConfiguration, CachedSettingsService cachedSettingsService, ICreditEnvSettings envSettings) : base(clientConfiguration, cachedSettingsService, envSettings)
        {
        }

        protected override string GetAppSetting(string name, bool mustExist) =>
            NTechEnvironment.Instance.Setting(name, mustExist);

        protected override FileInfo GetClientResourceFile(string settingName, string resourceFolderRelativePath, bool mustExist) =>
            NTechEnvironment.Instance.ClientResourceFile(settingName, resourceFolderRelativePath, mustExist);
    }
}