using NTech.Banking.CivicRegNumbers.Fi;
using NTech.Banking.CivicRegNumbers.Se;
using System;
using System.Linq;

namespace NTech.Banking.CivicRegNumbers
{
    public class CivicRegNumberGenerator
    {
        private string country;

        public bool UseReformedFinnishCenturyMarkers { get; set; } = false;

        public CivicRegNumberGenerator(string country)
        {
            this.country = country;
        }        

        public ICivicRegNumber Generate(Random r, DateTime? birthDate = null)
        {
            if(birthDate == null)
            {
                var ageModifier = 20 + (int)((r.NextDouble() * 60d)); //20 -> 80
                birthDate = DateTime.Today.AddYears(-ageModifier);
            }
            if (country == "FI")
            {
                var bd = birthDate.Value;
                var seqNr = r.Next(2, 899);
                var prefix = $"{bd.ToString("ddMMyy")}{GenerateFinnishCenturyMarker(r, bd.Year)}{seqNr.ToString().PadLeft(3, '0')}";
                return CivicRegNumberFi.Parse(prefix + CivicRegNumberFi.GenerateCheckDigit(prefix));
            }
            else if (country == "SE")
            {
                var bd = birthDate.Value;
                var seqNr = r.Next(1, 999);
                var checkDigit = CivicRegNumberSe.ComputeMod10CheckDigit($"{bd.ToString("yyMMdd")}{seqNr.ToString().PadLeft(3, '0')}").ToString();
                return CivicRegNumberSe.Parse($"{bd.ToString("yyyyMMdd")}{seqNr.ToString().PadLeft(3, '0')}{checkDigit}");
            }
            else
                throw new NotImplementedException();
        }

        private string GenerateFinnishCenturyMarker(Random r, int year)
        {
            if (UseReformedFinnishCenturyMarkers)
            {
                var centuryMarkers = (year < 1900
                    ? CivicRegNumberFi.CenturyMarker18
                    : (year < 2000 ? CivicRegNumberFi.CenturyMarker19 : CivicRegNumberFi.CenturyMarker20)).ToList();

                return Convert.ToString(centuryMarkers[r.Next(centuryMarkers.Count)]);
            }
            else
                return year >= 2000 ? "A" : "-";
        }
    }
}
