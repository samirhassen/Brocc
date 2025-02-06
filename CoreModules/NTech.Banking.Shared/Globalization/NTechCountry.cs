using NTech.Banking.Conversion;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace NTech.Banking.Shared.Globalization
{
    public class NTechCountry : IEquatable<NTechCountry>
    {
        private static readonly Lazy<(Dictionary<string, NTechCountry> TwoLetterIsoCodeToCountry, Dictionary<string, NTechCountry> ThreeLetterIsoCodeToCountry)> Cache = new Lazy<(Dictionary<string, NTechCountry> TwoLetterIsoCodeToCountry, Dictionary<string, NTechCountry> ThreeLetterIsoCodeToCountry)>(
            () =>
            {
                var result = (TwoLetterIsoCodeToCountry: new Dictionary<string, NTechCountry>(StringComparer.OrdinalIgnoreCase), ThreeLetterIsoCodeToCountry: new Dictionary<string, NTechCountry>(StringComparer.OrdinalIgnoreCase));

                var windowsIncludeIsos = new HashSet<string>();
                //Based on https://stackoverflow.com/questions/4884692/converting-country-codes-in-net
                foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
                {
                    var region = new RegionInfo(culture.LCID);
                    if (region.TwoLetterISORegionName.Any(char.IsDigit))
                        continue; //Windows includes some broken wierd stuff here

                    var c = new NTechCountry(region);
                    result.TwoLetterIsoCodeToCountry[c.TwoLetterIsoCountryCode] = c;
                    result.ThreeLetterIsoCodeToCountry[c.ThreeLetterIsoCountryCode] = c;

                    windowsIncludeIsos.Add(c.TwoLetterIsoCountryCode);
                }

                EmbeddedResources.WithEmbeddedStream("NTech.Banking.Shared.Globalization", "ExtraCountries.xml", stream =>
                {
                    var countries = XDocument.Load(stream).Descendants().Where(x => x.Name.LocalName == "c").ToList();
                    foreach(var country in countries)
                    {
                        var iso2 = country.Attribute("a2").Value;
                        if (!windowsIncludeIsos.Contains(iso2))
                        {
                            var c = new NTechCountry(iso2, country.Attribute("a3").Value, country.Attribute("en").Value);
                            result.TwoLetterIsoCodeToCountry[c.TwoLetterIsoCountryCode] = c;
                            result.ThreeLetterIsoCodeToCountry[c.ThreeLetterIsoCountryCode] = c;
                        }
                    }

                    return (object)null;
                });

                return result;
            });

        private readonly string twoLetterIsoName;
        private readonly string threeLetterIsoName;
        private readonly string englishName;

        private NTechCountry(RegionInfo region) 
            : this(region.TwoLetterISORegionName, region.ThreeLetterISORegionName, region.EnglishName)
        {
            
        }

        private NTechCountry(string twoLetterIsoName, string threeLetterIsoName, string englishName)
        {
            this.twoLetterIsoName = twoLetterIsoName;
            this.threeLetterIsoName = threeLetterIsoName;
            this.englishName = englishName;
        }

        public static NTechCountry FromTwoLetterIsoCode(string isoCode, bool returnNullWhenNotExists = false)
        {
            if (!Cache.Value.TwoLetterIsoCodeToCountry.TryGetValue(isoCode ?? "", out var result))
            {
                if (returnNullWhenNotExists)
                    return null;
                throw new ArgumentException($"No such country two letter country code '{isoCode}'", nameof(isoCode));
            }
            return result;
        }

        public static NTechCountry FromThreeLetterIsoCode(string isoCode, bool returnNullWhenNotExists = false)
        {
            if (!Cache.Value.ThreeLetterIsoCodeToCountry.TryGetValue(isoCode ?? "", out var result))
            {
                if (returnNullWhenNotExists)
                    return null;
                throw new ArgumentException($"No such country three letter country code '{isoCode}'", nameof(isoCode));
            }
            return result;
        }

        public static NTechCountry FromCultureInfo(CultureInfo cultureInfo)
        {
            return FromTwoLetterIsoCode(new RegionInfo(cultureInfo.LCID).TwoLetterISORegionName);
        }

        public static ISet<string> AllTwoLetterIsoCountryCodes =>
            new HashSet<string>(Cache.Value.TwoLetterIsoCodeToCountry.Keys);

        public static ISet<string> AllThreeLetterIsoCountryCodes =>
            new HashSet<string>(Cache.Value.ThreeLetterIsoCodeToCountry.Keys);

        public string TwoLetterIsoCountryCode => this.twoLetterIsoName;
        public string ThreeLetterIsoCountryCode => this.threeLetterIsoName;
        public string EnglishName => this.englishName;

        #region "Equality boilerplate"
        public override bool Equals(object other)
        {
            return this.Equals(other as NTechCountry);
        }

        public bool Equals(NTechCountry other)
        {
            if (Object.ReferenceEquals(other, null))
                return false;
            
            if (Object.ReferenceEquals(this, other))
                return true;

            if (this.GetType() != other.GetType())
                return false;

            return TwoLetterIsoCountryCode == other.TwoLetterIsoCountryCode;
        }

        public static bool operator ==(NTechCountry c1, NTechCountry c2)
        {
            if (Object.ReferenceEquals(c1, null))
            {
                return Object.ReferenceEquals(c2, null);
            }

            return c1.Equals(c2);
        }

        public static bool operator !=(NTechCountry c1, NTechCountry c2)
        {
            return !(c1 == c2);
        }


        public override int GetHashCode()
        {
            return TwoLetterIsoCountryCode.GetHashCode();
        }

        public override string ToString()
        {
            return TwoLetterIsoCountryCode;
        }
        #endregion
    }
}
