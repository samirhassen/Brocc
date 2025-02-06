using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Web;
using System.Xml.Linq;

namespace nGccCustomerApplication.Code
{
    public class BalanziaApplicationTranslation
    {
        public Dictionary<string, Dictionary<string, string>> GetTranslationTable(string filename)
        {
            Func<Dictionary<string, Dictionary<string, string>>> load = () =>
            {
                var t = GetTranslationTableI(filename);

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

            Dictionary<string, Dictionary<string, string>> table;
            if (NEnv.IsCachingEnabled)
            {
                var cacheKey = $"tr:{filename}";
                var c = MemoryCache.Default.Get(cacheKey);
                if (c == null)
                {
                    table = load();
                    MemoryCache.Default.Add(cacheKey, table, DateTimeOffset.Now.AddMinutes(30));
                }
                else
                {
                    table = (Dictionary<string, Dictionary<string, string>>)c;
                }
            }
            else
            {
                table = load();
            }
            return table;
        }

        private Dictionary<string, Dictionary<string, string>> GetTranslationTableI(string filename)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"nGccCustomerApplication.Resources.{filename}";
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
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
        }
    }
}