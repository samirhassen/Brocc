using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NTech.Services.Infrastructure
{
    public class NTechSimpleSettings
    {
        private readonly IDictionary<string, string> settings;
        private readonly string context;

        private NTechSimpleSettings(IDictionary<string, string> settings, string context)
        {
            this.settings = settings;
            this.context = context;
        }

        public string Opt(string name)
        {
            string v;
            if (settings.TryGetValue(name, out v))
                return v;
            else
                return null;
        }

        public bool OptBool(string name, bool defaultValue = false)
        {
            return (Opt(name) ?? (defaultValue ? "true" : "false")).Trim().ToLowerInvariant() == "true";
        }

        public bool ReqBool(string name)
        {
            Req(name);
            return OptBool(name);
        }

        public string Req(string name)
        {
            var v = Opt(name);
            if (v == null)
                throw new Exception($"Required setting '{name}' missing from {context}");
            return v;
        }

        /// <summary>
        /// - Default encoding is utf8
        /// - Settings can contain =
        /// - Settings cannot be multiple lines
        /// - Leading and trailing space on settings is ignored
        /// Example:
        /// #this is a comment
        /// setting1 = 3
        /// </summary>
        public static NTechSimpleSettings ParseSimpleSettingsFile(string filename, Encoding encoding = null, bool forceFileExistance = false)
        {
            var d = ParseSettings(filename, fn =>
            {
                var lines = File
                    .ReadAllLines(fn, encoding ?? Encoding.UTF8);

                return SimpleSettingsLinesToDictionary(lines);
            }, forceFileExistance);

            return new NTechSimpleSettings(d, $" simple settings file: '{filename}'");
        }

        public static string GetValueFromClientResourceFile(string filename, string value, string placeholderValue = null)
        {
            var metadataClientResourcesFile = NTechEnvironment.Instance.ClientResourceFile(value, filename, mustExist: false);
            if (metadataClientResourcesFile.Exists)
            {
                var settingsValueFromFile = ParseSimpleSettingsFile(metadataClientResourcesFile.FullName, forceFileExistance: false);
                var settingsValue = settingsValueFromFile.Opt(value);

                if (!string.IsNullOrEmpty(settingsValue))
                  return settingsValue;
            }

            if (!string.IsNullOrEmpty(placeholderValue))
                return placeholderValue; 
            
            return null; 
        }

        public static NTechSimpleSettings ParseAppSettingsFile(string filename, bool forceFileExistance = false)
        {
            var d = ParseSettings(filename, fn =>
            {
                var s = XDocument.Load(fn);
                var dd = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var root = s.Descendants().Where(x => x.Name.LocalName == "appSettings").SingleOrDefault();
                foreach (var y in root.Descendants().Where(x => x.Name.LocalName == "add"))
                {
                    var key = y.Attribute("key").Value;
                    var value = y.Attribute("value").Value;
                    if(!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                        dd[key.Trim()] = value.Trim();
                }
                return dd;
            }, forceFileExistance);

            return new NTechSimpleSettings(d, $" app settings file: '{filename}'");
        }

        private static Dictionary<string, string> ParseSettings(string filename, Func<string, Dictionary<string, string>> handleFile, bool forceFileExistance)
        {
            if (File.Exists(filename))
                return handleFile(filename);
            else if (forceFileExistance)
            {
                //This is not the default since it seems more natural to blow up when an actual setting is requested.
                throw new Exception($"Missing simple settings file '{filename}'");
            }
            else
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public static Dictionary<string, string> SimpleSettingsLinesToDictionary(IEnumerable<string> lines)
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in lines.Where(x => !string.IsNullOrWhiteSpace(x) && !x.Trim().StartsWith("#")).Select(x => x.Trim()))
            {
                var i = line.IndexOf('=');
                if (i >= 1 && i < (line.Length - 1))
                {
                    d[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim()?.Replace("[[[NEWLINE]]]", Environment.NewLine);
                }
                else
                    throw new Exception("Invalid settings file. Lines should have the format name=value");
            }
            return d;
        }
    }
}
