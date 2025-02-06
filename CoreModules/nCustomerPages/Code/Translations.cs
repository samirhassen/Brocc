using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace nCustomerPages
{
    public static class Translations
    {
        private static Dictionary<string, Dictionary<string, string>> GetTranslationTable()
        {
            Func<Dictionary<string, Dictionary<string, string>>> load = () =>
            {
                var t = GetTranslationTableI();

                if (!NEnv.IsProduction)
                {
                    //Add the key as translation for langs where it is missing to make fixing missing translations easier
                    var allKeys = t.SelectMany(x => x.Value.Select(y => y.Key)).Distinct();
                    foreach (var key in allKeys)
                    {
                        foreach (var lang in t.Keys)
                        {
                            if (!t[lang].ContainsKey(key))
                            {
                                t[lang][key] = $"(*){key}";
                            }
                        }
                    }
                }

                return t;
            };

            return NEnv.IsTranslationCacheDisabled
                ? load()
                : NTechCache.WithCache($"tr:Translations.xml", TimeSpan.FromMinutes(30), load);
        }

        public static Dictionary<string, string> GetTranslationTable(string lang)
        {
            var table = GetTranslationTable();

            if (table.ContainsKey(lang))
            {
                return table[lang];
            }
            else
            {
                return null;
            }
        }

        private static Dictionary<string, Dictionary<string, string>> GetTranslationTableI()
        {
            Stream stream = null;
            try
            {
                var localFileOverride = NEnv.TranslationOverrideEmbeddedFileWithThisLocalFilePath;
                if (localFileOverride != null && File.Exists(localFileOverride))
                {
                    stream = File.OpenRead(localFileOverride);
                }
                else
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    var resourceName = $"nCustomerPages.Resources.Translations.xml";
                    stream = assembly.GetManifestResourceStream(resourceName);
                }
                XDocument doc = XDocuments.Load(stream);

                var translationTable = new Dictionary<string, Dictionary<string, string>>();

                foreach (var tr in doc.Descendants().Where(x => x.Name.LocalName == "tr"))
                {
                    var key = tr.Elements().Single(x => x.Name.LocalName == "key").Value;
                    foreach (var ttr in tr.Elements().Where(x => x.Name.LocalName != "key"))
                    {
                        var ttrLang = ttr.Name.LocalName;
                        if (!translationTable.ContainsKey(ttrLang))
                        {
                            translationTable.Add(ttrLang, new Dictionary<string, string>());
                        }
                        var langTable = translationTable[ttrLang];
                        if (!string.IsNullOrWhiteSpace(ttr.Value))
                            langTable[key] = ttr.Value.Trim();
                    }
                }

                return translationTable;
            }
            finally
            {
                stream?.Dispose();
            }
        }

        public static dynamic FetchTranslation(string lang)
        {
            var t = GetTranslationTable(lang);

            if (t == null)
                return null;

            dynamic exp = new ExpandoObject();
            var d = (IDictionary<string, object>)exp;
            foreach (var r in t)
            {
                d[r.Key] = r.Value;
            }
            return exp;
        }
    }
}