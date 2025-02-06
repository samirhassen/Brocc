using System;
using System.Globalization;
using System.Linq;

namespace nCreditReport.Code.UcSe
{
    internal static class UcExtensions
    {
        public static string OptSingleValue(this UcSeService2.group source, string itemName)
        {
            var v = source?.term?.Where(x => x.id.Equals(itemName, StringComparison.OrdinalIgnoreCase))?.FirstOrDefault()?.Value;
            if (string.IsNullOrWhiteSpace(v))
                return null;
            else
                return v?.Trim();
        }

        public static T OptSingleValue<T>(this UcSeService2.group source, string itemName, Func<string, T> parse)
        {
            var v = OptSingleValue(source, itemName);
            if (v == null)
                return default(T);
            else
                return parse(v);
        }

        public static decimal? OptSinglePercentValue(this UcSeService2.group source, string itemName)
        {
            return source.OptSingleValue(itemName, y => new decimal?(decimal.Parse(y?.Replace(",", "."), CultureInfo.InvariantCulture)));
        }

        public static int? OptSingleIntValue(this UcSeService2.group source, string itemName)
        {
            return OptSingleValue(source, itemName, x => new int?(int.Parse(x)));
        }

        /// <summary>
        /// YYYYMMDD
        /// </summary>
        public static DateTime? OptLongDate(this UcSeService2.group source, string itemName)
        {
            return OptSingleValue(source, itemName, x =>
                x.Where(char.IsDigit).Count() != 8 ? new DateTime?() :
                new DateTime(
                    int.Parse(x.Substring(0, 4)),
                    int.Parse(x.Substring(4, 2)),
                    int.Parse(x.Substring(6, 2)))
            );
        }
    }
}