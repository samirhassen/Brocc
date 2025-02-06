using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech
{
    public static class Numbers
    {
        public static int? ParseInt32OrNull(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;
            if (int.TryParse(input, out var v))
                return v;
            return 
                null;
        }

        public static decimal? ParseDecimalOrNull(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            decimal d;
            if (decimal.TryParse(input?.Replace(",", ".")?.Trim(), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out d))
            {
                return d;
            }
            return null;
        }
    }
}
