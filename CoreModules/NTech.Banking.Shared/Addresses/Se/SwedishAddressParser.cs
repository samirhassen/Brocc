using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NTech.Banking.BankAccounts.Addresses.Se
{
    public class SwedishAddressParser : ISwedishAddressParser
    {
        public Tuple<string, string> SplitStreetAddressIntoAddressAndLghNr(string streetAddress)
        {
            if (streetAddress == null)
                return null;

            int hitCount = 0;
            string lghNr = null;
            var streetOnly = Regex.Replace(streetAddress, @"lgh\s(\d\d\d\d)", m =>
                {
                    hitCount++;
                    lghNr = m.Groups[1].Value;
                    return "";
                }, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)?.Trim();
            if (hitCount == 1)
                return Tuple.Create(streetOnly, lghNr);
            else
                return null;
        }
    }
}
