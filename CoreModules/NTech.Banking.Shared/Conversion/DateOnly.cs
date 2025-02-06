using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech
{
    /// <summary>
    /// Date without timezone or time to avoid problems with timezone switching and similar since we often track transactions dates without time in banking contexts.
    /// This is especially bad when going back and forth between cs and js so this class is designed to be easy to serialize and deserialize (hence breaking the naming convention of year, month, day to lowercase)
    /// 
    /// This is a class rather than a struct since when living only in c# structs will keep you from dealing with null but when having serialization added to the mix structs just make it worse as null
    /// is masked as year = 0, month = 0, day = 0 instead of null.
    /// </summary>
    public class DateOnly: IComparable<DateOnly>, IEquatable<DateOnly>
    {        
        public int yearMonthDay { get; set; }

        private void Validate()
        {
            DateTime _;
            if (!(DateTime.TryParseExact(yearMonthDay.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)))
                throw new Exception($"Invalid date '{yearMonthDay}'");
        }

        public static DateOnly Create(int year, int month, int day)
        {
            var d = new DateOnly
            {
                yearMonthDay = year * 10000 + month * 100 + day
            };
            d.Validate();
            return d;
        }

        public int Day()
        {
            return yearMonthDay % 100;
        }
        
        public int Month()
        {
            return ((yearMonthDay - yearMonthDay % 100) / 100) % 100;
        }

        public int Year()
        {
            var tmp = ((yearMonthDay - yearMonthDay % 100) / 100); // yyyymm
            return (tmp - (tmp % 100)) / 100;
        }

        public static DateOnly Create(DateTime? d)
        {
            return d.HasValue ? Create(d.Value.Year, d.Value.Month, d.Value.Day) : null;
        }

        public static DateOnly Create(DateTimeOffset? d)
        {
            return d.HasValue ? Create(d.Value.Year, d.Value.Month, d.Value.Day) : null;
        }

        public DateTime ToDate()
        {
            return new DateTime(Year(), Month(), Day());
        }

        /// <summary>
        /// Date as a number
        /// </summary>
        /// <returns>The as an int on the format yyyymmdd</returns>
        public int ToInt()
        {
            return yearMonthDay;
        }

        public override string ToString()
        {
            return $"{Year()}-{Month().ToString().PadLeft(2, '0')}-{Day().ToString().PadLeft(2, '0')}";
        }

        public override int GetHashCode()
        {
            return ToInt().GetHashCode();
        }

        private static int CompareTo(DateOnly x, DateOnly y)
        {
            //https://ericlippert.com/2013/10/28/math-from-scratch-part-ten-integer-comparisons/
            var ix = x?.ToInt() ?? 0;
            var iy = y?.ToInt() ?? 0;
            if (ix < 0 && iy > 0)
                return -1;
            else if (ix > 0 && iy < 0)
                return 1;
            else if (ix > 0)
                return ix.CompareTo(iy);
            else
                return iy.CompareTo(ix);
        }

        public int CompareTo(DateOnly other) { return CompareTo(this, other); }
        public bool Equals(DateOnly other) { return CompareTo(this, other) == 0; }
        public static bool operator <(DateOnly x, DateOnly y) { return CompareTo(x, y) < 0; }
        public static bool operator >(DateOnly x, DateOnly y) { return CompareTo(x, y) > 0; }
        public static bool operator <=(DateOnly x, DateOnly y) { return CompareTo(x, y) <= 0; }
        public static bool operator >=(DateOnly x, DateOnly y) { return CompareTo(x, y) >= 0; }
        public static bool operator ==(DateOnly x, DateOnly y) { return CompareTo(x, y) == 0; }
        public static bool operator !=(DateOnly x, DateOnly y) { return CompareTo(x, y) != 0; }
        public override bool Equals(object obj)
        {
            return (obj is DateOnly) && (CompareTo(this, (DateOnly)obj) == 0);
        }
    }
}
