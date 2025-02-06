using NTech.Core.PreCredit.Shared.Services;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace nPreCredit
{
    public static class Translations
    {
        public static Dictionary<string, Dictionary<string, string>> GetTranslationTable()
        {
            Func<Dictionary<string, Dictionary<string, string>>> load = () =>
            {
                var t = GetEmbeddedXmlTranslationTable("Translations.xml");

                Action<string> loadSimpleTranslationFile = lang =>
                {
                    if (!t.ContainsKey(lang))
                        t.Add(lang, new Dictionary<string, string>());

                    foreach (var k in WithEmbeddedStream($"translations-{lang}.txt", s => NTechSimpleSettings.SimpleSettingsLinesToDictionary(Streams.ReadAllLines(s))))
                    {
                        t[lang][k.Key] = k.Value;
                    }
                };

                loadSimpleTranslationFile("sv");
                loadSimpleTranslationFile("en");

                if (NEnv.IsMortgageLoansEnabled && t.ContainsKey("en"))
                {
                    //TODO: Remove this eventually. This is a transition hack to allow the swedish only translations for mortage concepts from translations-sv.txt to show up in the english backoffice
                    var en = t["en"];

                    foreach (var kv in t["sv"])
                    {
                        if (!en.ContainsKey(kv.Key))
                            en[kv.Key] = kv.Value;
                    }
                }

                return t;
            };
            if (NEnv.IsTranslationCacheDisabled)
                return load();
            else
                return NTechCache.WithCache($"tr:application-translations", TimeSpan.FromMinutes(30), load);
        }

        private static Dictionary<string, Dictionary<string, string>> GetEmbeddedXmlTranslationTable(string filename)
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
            return load();
        }

        private static T WithEmbeddedStream<T>(string filename, Func<Stream, T> f) => TranslationService.WithEmbeddedStream(filename, f);
        private static Dictionary<string, Dictionary<string, string>> GetTranslationTableI(string filename)
        {
            return WithEmbeddedStream(filename, stream =>
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
            });
        }

        public static dynamic FetchTranslation(string lang)
        {
            Func<Dictionary<string, string>, dynamic> tableToAngularTranslateResource = t =>
            {
                dynamic exp = new ExpandoObject();
                var d = (IDictionary<string, object>)exp;
                foreach (var r in t)
                {
                    d[r.Key] = r.Value;
                }
                return exp;
            };

            var table = GetTranslationTable();

            if (table.ContainsKey(lang))
            {
                return tableToAngularTranslateResource(table[lang]);
            }
            else
            {
                return null;
            }
        }

        public static string GetTranslation(string name, string preferredLanguage, Action observeIsMissingTranslation = null)
        {
            var table = NTechCache.WithCache(
                "1baf01a3-e336-4ab0-86a6-55592341b314",
                TimeSpan.FromMinutes(30),
                () => GetTranslationTable());

            Func<string, string, string> fetch = (n, lang) =>
            {
                var t = table.Opt(lang);
                return t?.Opt(n);
            };

            var result = fetch(name, preferredLanguage);
            if (string.IsNullOrWhiteSpace(result))
                result = fetch(name, "en");
            if (string.IsNullOrWhiteSpace(result))
            {
                observeIsMissingTranslation?.Invoke();
                result = name;
            }
            return result;
        }
    }
}