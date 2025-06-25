using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace NTech.Services.Infrastructure
{
    public class ClientConfiguration : IClientConfiguration
    {
        private readonly string clientName;
        private readonly IDictionary<string, string> settings;
        private readonly HashSet<string> activeFeatures;
        private readonly XElement root;

        private ClientConfiguration(XDocument cfg)
        {
            root = cfg.Root;

            clientName = root.Attribute("clientName").Value;
            settings = root
                .Elements()
                .Where(x => x.Name.LocalName == "Settings")
                .SelectMany(x => x.Elements().Where(y => y.Name.LocalName == "Setting"))
                .ToDictionary(x => x.Attribute("key").Value, x => x.Attribute("value").Value,
                    StringComparer.InvariantCultureIgnoreCase);

            var countrySetupElement = root.Elements().SingleOrDefault(x => x.Name.LocalName == "CountrySetup");
            if (countrySetupElement == null)
            {
                Country = new CountrySetup
                {
                    BaseCountry = "FI",
                    BaseCurrency = "EUR",
                    BaseFormattingCulture = "fi-FI"
                };
            }
            else
            {
                Country = new CountrySetup
                {
                    BaseCurrency = countrySetupElement.Elements().Single(x => x.Name.LocalName == "BaseCurrency").Value
                        .Trim().ToUpper(),
                    BaseCountry = countrySetupElement.Elements().Single(x => x.Name.LocalName == "BaseCountry").Value
                        .Trim().ToUpper(),
                    BaseFormattingCulture = countrySetupElement.Elements()
                        .Single(x => x.Name.LocalName == "BaseFormattingCulture").Value.Trim(),
                };
            }

            activeFeatures = new HashSet<string>(root
                .Elements()
                .Where(x => x.Name.LocalName == "ActiveFeatures")
                .SelectMany(x =>
                    x.Elements().Where(y => y.Name.LocalName == "Feature").Select(y => y.Attribute("name")?.Value))
                .Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.InvariantCultureIgnoreCase);
        }

        public XElement GetCustomSection(string name)
        {
            return root
                .Descendants()
                .SingleOrDefault(x => x.Name.LocalName == name);
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
                containerElement = containerElement.Descendants()
                    .SingleOrDefault(x => x.Name.LocalName == elementName);
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
                containerElement = containerElement.Descendants()
                    .SingleOrDefault(x => x.Name.LocalName == elementName);
                if (containerElement != null) continue;

                if (mustExist)
                    throw new Exception($"Missing in ClientConfiguration: {string.Join(".", elementPath)}");

                return null;
            }

            if (string.IsNullOrWhiteSpace(containerElement.Value))
            {
                if (mustExist)
                    throw new Exception($"Missing in ClientConfiguration: {string.Join(".", elementPath)}");

                return null;
            }

            return containerElement.Value.Trim();
        }

        public int? GetSingleCustomInt(bool mustExist, params string[] elementPath)
        {
            var value = GetSingleCustomValue(mustExist, elementPath);
            if (value == null)
                return null;
            return int.Parse(value);
        }

        public bool? GetSingleCustomBoolean(bool mustExist, params string[] elementPath)
        {
            var value = GetSingleCustomValue(mustExist, elementPath);
            if (value == null)
                return null;
            return value.ToLowerInvariant() == "true";
        }

        public decimal? GetSingleCustomDecimal(bool mustExist, params string[] elementPath)
        {
            var value = GetSingleCustomValue(mustExist, elementPath);
            if (value == null)
                return null;
            return decimal.Parse(value, CultureInfo.InvariantCulture);
        }

        public static ClientConfiguration CreateUsingNTechEnvironment(NTechEnvironment env = null)
        {
            env = env ?? NTechEnvironment.Instance;
            var fn = env.Setting("ntech.clientcfgfile", false);
            if (fn != null) return new ClientConfiguration(XDocument.Load(fn));

            var f = env.Setting("ntech.clientresourcefolder", false);
            if (f == null)
                throw new Exception("Missing appsetting 'ntech.clientcfgfile'");

            fn = Path.Combine(f, "ClientConfiguration.xml");

            return new ClientConfiguration(XDocument.Load(fn));
        }

        public static ClientConfiguration CreateUsingXDocument(XDocument cfg)
        {
            return new ClientConfiguration(cfg);
        }

        public CountrySetup Country { get; }

        public string ClientName
        {
            get
            {
                if (clientName == null)
                    throw new Exception("Invalid client configuration. Missing clientName");
                return clientName;
            }
        }

        public string OptionalSetting(string name)
        {
            if (!settings.TryGetValue(name, out var v)) return null;
            return string.IsNullOrWhiteSpace(v) ? null : v.Trim();
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
                switch (BaseCountry)
                {
                    case "FI":
                        return "fi";
                    case "SE":
                        return "sv";
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }

    public interface IClientConfiguration
    {
        ClientConfiguration.CountrySetup Country { get; }
        string ClientName { get; }
        string OptionalSetting(string name);
        XElement GetCustomSection(string name);
        bool IsFeatureEnabled(string name);
    }
}