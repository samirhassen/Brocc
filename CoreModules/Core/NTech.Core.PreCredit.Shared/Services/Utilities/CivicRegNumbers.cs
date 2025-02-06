using NTech.Banking.CivicRegNumbers;
using NTech.Core;
using System;

namespace nPreCredit.Code
{
    public static class CivicRegNumbers
    {
        public static int? ComputeAgeFromCivicRegNumber(string civicRegNr, ICoreClock clock, CivicRegNumberParser civicRegNumberParser)
        {
            if (string.IsNullOrWhiteSpace(civicRegNr))
                return null;
            if (!civicRegNumberParser.TryParse(civicRegNr, out var parsedNr))
                return null;
            return ComputeAgeFromCivicRegNumber(parsedNr, clock);
        }

        public static int? ComputeAgeFromCivicRegNumber(ICivicRegNumber civicRegNr, ICoreClock clock)
        {
            int? GetFullYearsSince(DateTime d)
            {
                var t = clock.Now.Date;
                if (t < d)
                    return null;

                var age = t.Year - d.Year;

                return (d.AddYears(age + 1) <= t) ? (age + 1) : age;
            }
            if (civicRegNr == null || !civicRegNr.BirthDate.HasValue)
                return null;
            return GetFullYearsSince(civicRegNr.BirthDate.Value);
        }
    }
}