using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech
{
    public static class Dates
    {
        public static DateTime? ParseDateTimeExactOrNull(string input, string format)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            DateTime d;
            if (DateTime.TryParseExact(input, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                return d;

            return null;
        }

        public static DateOnly ParseDateOnlyExactOrNull(string input, string format)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            DateTime d;
            if (DateTime.TryParseExact(input, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                return DateOnly.Create(d);
            
            return null;
        }

        public static int GetAbsoluteNrOfMonthsBetweenDates(DateTime date1, DateTime date2)
        {
            if(date1 < date2)
            {
                var tmp = date2;
                date2 = date1;
                date1 = tmp;
            }
            return (date1.Year - date2.Year) * 12 + date1.Month - date2.Month + (date1.Day >= date2.Day ? 0 : -1);
        }

        public static TimeSpan GetAbsoluteTimeBetween(DateTime d1, DateTime d2)
        {
            return d2 > d1 ? d2.Subtract(d1) : d1.Subtract(d2);
        }

        public static int GetAbsoluteNrOfDaysBetweenDates(DateTime d1, DateTime d2)
        {
            return (int)GetAbsoluteTimeBetween(d1, d2).TotalDays;
        }

        public static DateTime? MaxN(params DateTime?[] args)
        {
            var v = args.Where(x => x.HasValue).Select(x => x.Value).ToArray();
            if (v.Length == 0)
                return null;
            return Max(v);
        }

        public static DateTime Max(params DateTime[] args)
        {
            return args.Max();
        }

        public static DateTime Min(params DateTime[] args)
        {
            return args.Min();
        }

        public static DateTime GetNextDateWithDayNrAfterDate(int dayNr, DateTime date)
        {
            var m = date.Day < dayNr ? date : date.AddMonths(1);
            return new DateTime(m.Year, m.Month, dayNr);
        }
    }
}

namespace System
{
    public static class DateExtensions
    {
        private static DateTime StartOfWeek(DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

        public static DateTime MondayOfWeek(this DateTime dt)
        {
            return StartOfWeek(dt, DayOfWeek.Monday);
        }
    }
}
