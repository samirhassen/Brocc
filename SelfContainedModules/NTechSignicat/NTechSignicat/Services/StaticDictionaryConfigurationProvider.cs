using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace NTechSignicat
{
    public class StaticDictionaryConfigurationProvider : ConfigurationProvider, IConfigurationSource
    {
        private readonly IDictionary<string, string> settings;

        public StaticDictionaryConfigurationProvider(IDictionary<string, string> settings)
        {
            this.settings = settings;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return this;
        }

        public override void Load()
        {
            foreach (var item in settings)
            {
                Data.Add(item.Key, item.Value);
            }
        }

        public static IConfigurationSource CreateFromAppsettingsInClassicAppConfig(string appConfigFileName)
        {
            try
            {
                var appConfigFile = XDocument.Load(appConfigFileName);
                var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var appSettingsElement = appConfigFile.Descendants().Single(x => x.Name.LocalName == "appSettings");
                foreach (var s in appSettingsElement.Descendants().Where(x => x.Name.LocalName.ToLowerInvariant() == "add"))
                {
                    d.Add(s.Attribute("key").Value, s.Attribute("value").Value);
                }
                return new StaticDictionaryConfigurationProvider(d);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load '{appConfigFileName}'", ex);
            }
        }
    }
}
