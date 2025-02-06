using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Xml.Linq;

namespace NTech.Services.Infrastructure
{
    public class ClientConfiguration : IClientConfiguration
    {
        private string clientName;
        private IDictionary<string, string> settings;
        private HashSet<string> activeFeatures;
        private CountrySetup countrySetup;
        private XElement root;
        
        private ClientConfiguration(XDocument cfg)
        {
            root = cfg.Root;

            this.clientName = root.Attribute("clientName").Value;
            this.settings = root
                .Elements()
                .Where(x => x.Name.LocalName == "Settings")
                .SelectMany(x => x.Elements().Where(y => y.Name.LocalName == "Setting"))
                .ToDictionary(x => x.Attribute("key").Value, x => x.Attribute("value").Value, StringComparer.InvariantCultureIgnoreCase);

            var countrySetupElement = root.Elements().SingleOrDefault(x => x.Name.LocalName == "CountrySetup");
            if(countrySetupElement == null)
            {
                this.countrySetup = new CountrySetup
                {
                    BaseCountry = "FI",
                    BaseCurrency = "EUR",
                    BaseFormattingCulture = "fi-FI"
                };
            }
            else
            {
                this.countrySetup = new CountrySetup
                {
                    BaseCurrency = countrySetupElement.Elements().Single(x => x.Name.LocalName == "BaseCurrency").Value.Trim().ToUpper(),
                    BaseCountry = countrySetupElement.Elements().Single(x => x.Name.LocalName == "BaseCountry").Value.Trim().ToUpper(),
                    BaseFormattingCulture = countrySetupElement.Elements().Single(x => x.Name.LocalName == "BaseFormattingCulture").Value.Trim(),
                };
            }

            this.activeFeatures = new HashSet<string>(root
                .Elements()
                .Where(x => x.Name.LocalName == "ActiveFeatures")
                .SelectMany(x => x.Elements().Where(y => y.Name.LocalName == "Feature").Select(y => y.Attribute("name")?.Value))
                .Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.InvariantCultureIgnoreCase);
        }

        public XElement GetCustomSection(string name)
        {
            return root
                .Descendants()
                .Where(x => x.Name.LocalName == name)
                .SingleOrDefault();
        }

        /*
         GetCodeDisplayNameDictionary("Application", "CivilStatuses", "CivilStatus")
        on
         <Application>
            <CivilStatuses>
              <CivilStatus code="single" displayName="Single" />
              <CivilStatus code="live_together" displayName="Living together" />
            </CivilStatuses>
        </Application>
        yields
        { "single", "Single"}, { "live_together", "Living together"}
         */
        public Dictionary<string, string> GetCustomCodeDisplayNameDictionary(params string[] elementPath)
        {
            var containerElement = root;
            foreach(var elementName in elementPath.Take(elementPath.Length - 1))
            {
                containerElement = containerElement.Descendants().Where(x => x.Name.LocalName == elementName).SingleOrDefault();
                if(containerElement == null)
                    throw new Exception($"Missing in ClientConfiguration: {string.Join(".", elementPath)}");
            }
            return containerElement
                .Descendants()
                .Where(x => x.Name == elementPath.Last())
                .ToDictionary(x => x.Attribute("code").Value, x => x.Attribute("displayName").Value);
        }

        public string GetSingleCustomValue(bool mustExist, params string[] elementPath)
        {
            var containerElement = root;
            foreach(var elementName in elementPath)
            {
                containerElement = containerElement.Descendants().Where(x => x.Name.LocalName == elementName).SingleOrDefault();
                if(containerElement == null)
                {
                    if(mustExist)
                        throw new Exception($"Missing in ClientConfiguration: {string.Join(".", elementPath)}");
                    else
                        return null;
                }                    
            }
            if(string.IsNullOrWhiteSpace(containerElement.Value))
            {
                    if(mustExist)
                        throw new Exception($"Missing in ClientConfiguration: {string.Join(".", elementPath)}");
                    else
                        return null;
            }
            else
                return containerElement.Value.Trim();
        }

        public int? GetSingleCustomInt(bool mustExist, params string[] elementPath)
        {
            var value = GetSingleCustomValue(mustExist, elementPath);
            if(value == null)
                return null;
            else
                return int.Parse(value);
        }

        public bool? GetSingleCustomBoolean(bool mustExist, params string[] elementPath)
        {
            var value = GetSingleCustomValue(mustExist, elementPath);
            if(value == null)
                return null;
            else
                return value.ToLowerInvariant() == "true";
        }

        public decimal? GetSingleCustomDecimal(bool mustExist, params string[] elementPath)
        {
            var value = GetSingleCustomValue(mustExist, elementPath);
            if (value == null)
                return null;
            else
                return decimal.Parse(value, CultureInfo.InvariantCulture);
        }

        public static ClientConfiguration CreateUsingNTechEnvironment(NTechEnvironment env = null)
        {
            env = env ?? NTechEnvironment.Instance;
            var fn = env.Setting("ntech.clientcfgfile", false);
            if (fn == null)
            {
                var f = env.Setting("ntech.clientresourcefolder", false);
                if (f == null)
                    throw new Exception("Missing appsetting 'ntech.clientcfgfile'");
                else
                    fn = Path.Combine(f, "ClientConfiguration.xml");
            }
                
            return new ClientConfiguration(XDocument.Load(fn));
        }
        
        public static ClientConfiguration CreateUsingXDocument(XDocument cfg)
        {
            return new ClientConfiguration(cfg);
        }

        public CountrySetup Country
        {
            get
            {
                return this.countrySetup;
            }
        }
        
        public string ClientName
        {
            get
            {
                if (this.clientName == null)
                    throw new Exception("Invalid client configuration. Missing clientName");
                return this.clientName;
            }
        }

        public string OptionalSetting(string name)
        {
            if (settings.ContainsKey(name))
            {
                var v = settings[name];
                if (string.IsNullOrWhiteSpace(v))
                    return null;
                else
                    return v.Trim();
            }
            else
                return null;
        }

        public bool OptionalBool(string name)
        {
            return (OptionalSetting(name) ?? "false").ToLowerInvariant() == "true";
        }

        public string RequiredSetting(string name)
        {
            var v = OptionalSetting(name);
            if (v == null)
                throw new Exception($"Missing required client configuration setting {name}");
            return v;
        }

        public bool IsFeatureEnabled(string name)
        {
            return activeFeatures.Contains(name);
        }

        public ISet<string> ActiveFeatures => activeFeatures;
        public IDictionary<string, string> Settings => settings;

        public class CountrySetup
        {
            public string BaseCurrency { get; set; }
            public string BaseCountry { get; set; }
            public string BaseFormattingCulture { get; set; }

            public string GetBaseLanguage()
            {
                if (BaseCountry == "FI")
                    return "fi";
                else if (BaseCountry == "SE")
                    return "sv";
                else
                    throw new NotImplementedException();
            }
        }
    }

    public interface IClientConfiguration
    {
        ClientConfiguration.CountrySetup Country { get; }
        string ClientName { get; }
    }
}
