using nPreCredit;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace NTech.Core.PreCredit.Shared.Services
{
    public class TranslationService
    {
        private readonly IPreCreditEnvSettings envSettings;
        private static Lazy<FewItemsCache> cache = new Lazy<FewItemsCache>(() => new FewItemsCache());

        public TranslationService(IPreCreditEnvSettings envSettings)
        {
            this.envSettings = envSettings;
        }

        public Dictionary<string, Dictionary<string, string>> GetTranslationTable()
        {
            Func<Dictionary<string, Dictionary<string, string>>> load = () =>
            {
                var t = GetEmbeddedXmlTranslationTable("Translations.xml");

                Action<string> loadSimpleTranslationFile = lang =>
                {
                    if (!t.ContainsKey(lang))
                        t.Add(lang, new Dictionary<string, string>());

                    foreach (var k in WithEmbeddedStream($"translations-{lang}.txt", s => NTechSimpleSettingsCore.SimpleSettingsLinesToDictionary(s.ReadAllLines())))
                    {
                        t[lang][k.Key] = k.Value;
                    }
                };

                loadSimpleTranslationFile("sv");
                loadSimpleTranslationFile("en");

                if (envSettings.IsMortgageLoansEnabled && t.ContainsKey("en"))
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
            if (envSettings.IsTranslationCacheDisabled)
                return load();
            else
                return cache.Value.WithCache($"tr:application-translations", TimeSpan.FromMinutes(30), load);
        }

        private Dictionary<string, Dictionary<string, string>> GetEmbeddedXmlTranslationTable(string filename)
        {
            Func<Dictionary<string, Dictionary<string, string>>> load = () =>
            {
                var t = GetTranslationTableI(filename);

                if (!envSettings.IsProduction)
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

        public static T WithEmbeddedStream<T>(string filename, Func<Stream, T> f)
        {
            var resourceName = $"NTech.Core.PreCredit.Shared.Resources.{filename}";
            using (Stream stream = typeof(TranslationService).Assembly.GetManifestResourceStream(resourceName))
            {
                return f(stream);
            }
        }

        private Dictionary<string, Dictionary<string, string>> GetTranslationTableI(string filename)
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
    }

    public class PreCreditTranslationModel
    {
        public string UiLanguage { get; set; }
        public Dictionary<string, Dictionary<string, string>> Translations { get; set; }
    }
}