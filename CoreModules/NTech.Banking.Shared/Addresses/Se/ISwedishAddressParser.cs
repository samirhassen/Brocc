using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Banking.BankAccounts.Addresses.Se
{
    public interface ISwedishAddressParser
    {
        Tuple<string, string> SplitStreetAddressIntoAddressAndLghNr(string streetAddress);
    }
}
