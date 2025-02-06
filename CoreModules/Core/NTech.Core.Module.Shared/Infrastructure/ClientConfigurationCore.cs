using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace NTech.Core.Module.Shared.Infrastructure
{
    public class ClientConfigurationCore : IClientConfigurationCore
    {
        private string clientName;
        private IDictionary<string, string> settings;
        private HashSet<string> activeFeatures;
        private ClientConfigurationCoreCountry countrySetup;
        private XElement root;

        private ClientConfigurationCore(XDocument cfg)
        {
            root = cfg.Root;

            this.clientName = root.Attribute("clientName").Value;
            this.settings = root
                .Elements()
                .Where(x => x.Name.LocalName == "Settings")
                .SelectMany(x => x.Elements().Where(y => y.Name.LocalName == "Setting"))
                .ToDictionary(x => x.Attribute("key").Value, x => x.Attribute("value").Value, StringComparer.InvariantCultureIgnoreCase);

            var countrySetupElement = root.Elements().SingleOrDefault(x => x.Name.LocalName == "CountrySetup");
            if (countrySetupElement == null)
            {
                this.countrySetup = new ClientConfigurationCoreCountry
                {
                    BaseCountry = "FI",
                    BaseCurrency = "EUR",
                    BaseFormattingCulture = "fi-FI"
                };
            }
            else
            {
                this.countrySetup = new ClientConfigurationCoreCountry
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
            foreach (var elementName in elementPath.Take(elementPath.Length - 1))
            {
                containerElement = containerElement.Descendants().Where(x => x.Name.LocalName == elementName).SingleOrDefault();
                if (containerElement == null)
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
            foreach (var elementName in elementPath)
            {
                containerElement = containerElement.Descendants().Where(x => x.Name.LocalName == elementName).SingleOrDefault();
                if (containerElement == null)
                {
                    if (mustExist)
                        throw new Exception($"Missing in ClientConfiguration: {string.Join(".", elementPath)}");
                    else
                        return null;
                }
            }
            if (string.IsNullOrWhiteSpace(containerElement.Value))
            {
                if (mustExist)
                    throw new Exception($"Missing in ClientConfiguration: {string.Join(".", elementPath)}");
                else
                    return null;
            }
            else
                return containerElement.Value.Trim();
        }

        /// <summary>
        /// Example:
        /// <Foo>
        ///   <Bar>Value 1</Bar>
        ///   <Bar>Value 2</Bar>
        /// </Foo>
        /// 
        /// GetRepeatedCustomValue("Foo", "Bar") -> ["Value 1", "Value 2"]
        /// </summary>
        public List<string> GetRepeatedCustomValue(params string[] elementPath)
        {
            if (elementPath.Length < 2)
                throw new ArgumentException("elementPath must have at least two elements", nameof(elementPath));
            var containerPath = elementPath.Take(elementPath.Length - 1).ToList();

            var containerElement = root;
            foreach (var elementName in containerPath)
            {
                containerElement = containerElement.Descendants().Where(x => x.Name.LocalName == elementName).SingleOrDefault();
                if (containerElement == null)
                {
                    return new List<string>();
                }
            }

            var valueElementName = elementPath.Last();
            return containerElement.Descendants().Where(x => x.Name.LocalName == valueElementName && !string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Value.Trim()).ToList();
        }

        public int? GetSingleCustomInt(bool mustExist, params string[] elementPath)
        {
            var value = GetSingleCustomValue(mustExist, elementPath);
            if (value == null)
                return null;
            else
                return int.Parse(value);
        }

        public bool? GetSingleCustomBoolean(bool mustExist, params string[] elementPath)
        {
            var value = GetSingleCustomValue(mustExist, elementPath);
            if (value == null)
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

        public static ClientConfigurationCore CreateUsingXDocument(XDocument cfg)
        {
            return new ClientConfigurationCore(cfg);
        }

        public ClientConfigurationCoreCountry Country
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
    }
}
