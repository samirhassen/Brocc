using Newtonsoft.Json;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace NTech.Core.Module.Shared
{
    public class IsoCountry
    {
        public string CommonName { get; set; }
        public string NativeName { get; set; }
        public string Iso2Name { get; set; }
        public string Iso3Name { get; set; }
        public Dictionary<string, string> TranslatedNameByLang2 { get; set; }

        private static Lazy<FewItemsCache> cache = new Lazy<FewItemsCache>(() => new FewItemsCache());
        public static Dictionary<string, IsoCountry> CountryByIso2Name => cache.Value.WithCache("81453271-ec1f-44b3-a963-7268ad627ddc", TimeSpan.FromHours(1), () =>
        {
            var d = new Dictionary<string, IsoCountry>(StringComparer.OrdinalIgnoreCase);
            LoadEmbedded().ForEach(x => d[x.Iso2Name] = x);
            return d;
        });

        public static List<IsoCountry> LoadEmbedded()
        {
            //To refresh the country list or add languages use the tool at SelfContainedModules\IsoCountryFileGenerator
            return WithEmbeddedStream("NTech.Core.Module.Shared.Resources", "countries.json", stream =>
            {
                using (var sr = new StreamReader(stream, Encoding.UTF8))
                {
                    return JsonConvert.DeserializeObject<List<IsoCountry>>(sr.ReadToEnd());
                }
            });
        }

        private static T WithEmbeddedStream<T>(string namespaceName, string fileName, Func<Stream, T> f)
        {
            Assembly assembly = Assembly.GetAssembly(typeof(IsoCountry));
            string text = namespaceName + "." + fileName;
            using (Stream stream = assembly.GetManifestResourceStream(text))
            {
                if (stream == null)
                {
                    throw new Exception("No embedded resource named '" + text + "' found in assembly '" + assembly.FullName + "'");
                }

                return f(stream);
            }
        }

        public static IsoCountry FromTwoLetterIsoCode(string isoCode, bool returnNullWhenNotExists = false)
        {
            if (!CountryByIso2Name.TryGetValue(isoCode ?? "", out var value))
            {
                if (returnNullWhenNotExists)
                    return null;

                throw new ArgumentException("No such country two letter country code '" + isoCode + "'", "isoCode");
            }

            return value;
        }
    }
}
