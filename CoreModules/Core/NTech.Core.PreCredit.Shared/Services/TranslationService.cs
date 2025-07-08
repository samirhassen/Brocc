using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using nPreCredit;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Infrastructure;

namespace NTech.Core.PreCredit.Shared.Services
{
    public class TranslationService
    {
        private readonly IPreCreditEnvSettings envSettings;
        private static readonly Lazy<FewItemsCache> Cache = new Lazy<FewItemsCache>(() => new FewItemsCache());

        public TranslationService(IPreCreditEnvSettings envSettings)
        {
            this.envSettings = envSettings;
        }

        public Dictionary<string, Dictionary<string, string>> GetTranslationTable()
        {
            return envSettings.IsTranslationCacheDisabled
                ? Load()
                : Cache.Value.WithCache("tr:application-translations", TimeSpan.FromMinutes(30), Load);

            Dictionary<string, Dictionary<string, string>> Load()
            {
                var t = GetEmbeddedXmlTranslationTable("Translations.xml");

                LoadSimpleTranslationFile("sv");
                LoadSimpleTranslationFile("en");

                if (!envSettings.IsMortgageLoansEnabled || !t.TryGetValue("en", out var en)) return t;

                // TODO: Remove this eventually. This is a transition hack to allow the swedish only translations for
                // mortgage concepts from translations-sv.txt to show up in the english backoffice
                foreach (var kv in t["sv"].Where(kv => !en.ContainsKey(kv.Key)))
                {
                    en[kv.Key] = kv.Value;
                }

                return t;

                void LoadSimpleTranslationFile(string lang)
                {
                    if (!t.ContainsKey(lang)) t.Add(lang, new Dictionary<string, string>());

                    foreach (var k in WithEmbeddedStream($"translations-{lang}.txt",
                                 s => NTechSimpleSettingsCore.SimpleSettingsLinesToDictionary(s.ReadAllLines())))
                    {
                        t[lang][k.Key] = k.Value;
                    }
                }
            }
        }

        private Dictionary<string, Dictionary<string, string>> GetEmbeddedXmlTranslationTable(string filename)
        {
            var t = GetTranslationTableI(filename);

            if (envSettings.IsProduction) return t;

            // Add the key as translation for languages where it is missing to make fixing missing translations easier
            var allKeys = t.SelectMany(x => x.Value.Select(y => y.Key)).Distinct();
            foreach (var key in allKeys)
            {
                foreach (var lang in t.Keys.Where(lang => !t[lang].ContainsKey(key)))
                {
                    t[lang][key] = $"(*){key}";
                }
            }

            return t;
        }

        public static T WithEmbeddedStream<T>(string filename, Func<Stream, T> f)
        {
            var resourceName = $"NTech.Core.PreCredit.Shared.Resources.{filename}";
            using (var stream = typeof(TranslationService).Assembly.GetManifestResourceStream(resourceName))
            {
                return f(stream);
            }
        }

        private static Dictionary<string, Dictionary<string, string>> GetTranslationTableI(string filename)
        {
            return WithEmbeddedStream(filename, stream =>
            {
                var doc = XDocuments.Load(stream);

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
                        if (tr.Attribute("allowWhitespace") != null)
                            langTable[key] = ttr.Value;
                        else if (!string.IsNullOrWhiteSpace(ttr.Value))
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