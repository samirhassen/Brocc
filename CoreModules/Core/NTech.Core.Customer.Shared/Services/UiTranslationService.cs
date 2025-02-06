using NTech.Core.Module;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reflection;

namespace NTech.Core.Customer.Shared.Services
{
    public class UiTranslationService
    {
        public UiTranslationService(Func<bool> isTranslationCacheDisabled, IClientConfigurationCore clientConfiguration)
        {
            this.isTranslationCacheDisabled = isTranslationCacheDisabled;
            this.clientConfiguration = clientConfiguration;
        }
        private static Lazy<FewItemsCache> cache = new Lazy<FewItemsCache>();
        private readonly Func<bool> isTranslationCacheDisabled;
        private readonly IClientConfigurationCore clientConfiguration;

        public (IDictionary<string, object> Translations, string UiLanguage) GetTranslations(string userLang)
        {
            if (isTranslationCacheDisabled())
                return GetTranslationsI(userLang);
            else
                return cache.Value.WithCache("ntech.customer.GetTranslations.{userLang}", TimeSpan.FromHours(1), () => GetTranslationsI(userLang));
        }

        private (IDictionary<string, object> Translations, string UiLanguage) GetTranslationsI(string userLang)
        {
            var p = new ExpandoObject();
            var pd = p as IDictionary<string, object>;

            var uiLanguage = "en";

            var enTranslation = FetchTranslation("en");
            pd["en"] = enTranslation;

            if (userLang == "fi" || userLang == "sv")
            {
                uiLanguage = userLang;
                var tr = FetchTranslation(userLang);
                pd[userLang] = tr ?? enTranslation;
            }

            return (Translations: p, UiLanguage: uiLanguage);
        }

        public Dictionary<string, Dictionary<string, string>> GetTranslationTable()
        {
            Func<Dictionary<string, Dictionary<string, string>>> load = () =>
            {
                var t = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

                string fallbackLang;
                if (clientConfiguration.Country.BaseCountry == "SE")
                    fallbackLang = "sv";
                else if (clientConfiguration.Country.BaseCountry == "FI")
                    fallbackLang = "fi";
                else
                    throw new NotImplementedException();

                t["en"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                t[fallbackLang] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var k in WithEmbeddedStream("translations-en.txt", s => NTechSimpleSettingsCore.SimpleSettingsLinesToDictionary(s.ReadAllLines())))
                {
                    t["en"][k.Key] = k.Value;
                    t[fallbackLang][k.Key] = k.Value; //Remove this if we ever add a separate translation
                }

                return t;
            };
            if (isTranslationCacheDisabled())
                return load();
            else
                return cache.Value.WithCache($"tr:customer-translations", TimeSpan.FromMinutes(30), load);
        }

        private T WithEmbeddedStream<T>(string filename, Func<Stream, T> f)
        {
            var assembly = Assembly.GetAssembly(typeof(UiTranslationService));
            var resourceName = $"NTech.Core.Customer.Shared.Resources.{filename}";
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                return f(stream);
            }
        }

        public IDictionary<string, object> FetchTranslation(string lang)
        {
            Func<Dictionary<string, string>, IDictionary<string, object>> tableToAngularTranslateResource = t =>
            {
                var exp = new ExpandoObject();
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
    }
}