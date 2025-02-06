using System;
using System.Globalization;

namespace NTech.Services.Infrastructure.NTechWs
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DateWithoutTimeAttribute : NTechWsStringValidationAttributeBase
    {
        /// <summary>
        /// Lets the user input YYYY-MM instead of YYYY-MM-DD
        /// </summary>
        public bool AllowMonthOnly { get; set; }

        protected override bool IsValidString(string value) => TryParseDateWithoutTime(value, out _, allowMonthOnly: AllowMonthOnly);

        public static bool TryParseDateWithoutTime(string value, out DateTime result, bool allowMonthOnly = false)
        {
            if (allowMonthOnly && value?.Length == 7)
                value = value + "-01";
            return DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
        }
        public static DateTime? ParseDateWithoutTimeOrNull(string value, bool allowMonthOnly = false) =>
            TryParseDateWithoutTime(value, out var d, allowMonthOnly: allowMonthOnly) ? d : new DateTime?();

    }
}
