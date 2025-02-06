using NTech.Banking.CivicRegNumbers.Fi;
using NTech.Banking.CivicRegNumbers.Se;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NTech.Banking.CivicRegNumbers
{
    public class CivicRegNumberGenerator
    {
        private string country;

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
                var prefix = $"{bd.ToString("ddMMyy")}{(bd.Year >= 2000 ? "A" : "-")}{seqNr.ToString().PadLeft(3, '0')}";
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
    }
}
