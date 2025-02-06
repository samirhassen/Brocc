using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.CivicRegNumbers
{
    public interface ICivicRegNumber
    {
        string NormalizedValue { get; }
        string CrossCountryStorageValue { get; }
        string Country { get; }
        DateTime? BirthDate { get; }
        bool? IsMale { get; }
    }
}
