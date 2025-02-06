using System;
using System.Globalization;

namespace NTech.Core.Module.Shared.Infrastructure
{
    public static class NTechCoreFormatting
    {
        private static Lazy<CultureInfo> SeDefaultCulture = new Lazy<CultureInfo>(() =>
        {
            var c = new CultureInfo("sv-SE");

            c.NumberFormat.CurrencyGroupSeparator = " ";

            return c;
        });

        public static CultureInfo GetPrintFormattingCulture(string cultureName)
        {
            return GetCulture(cultureName);
        }

        public static CultureInfo GetScreenFormattingCulture(string cultureName)
        {
            return GetCulture(cultureName);
        }

        public static CultureInfo GetCulture(string cultureName)
        {
            if (cultureName == null)
                throw new ArgumentNullException("cultureName");

            if (cultureName.Equals("sv-SE", StringComparison.OrdinalIgnoreCase))
                return SeDefaultCulture.Value;
            else
                return CultureInfo.GetCultureInfo(cultureName);
        }

        public static string FormatMonth(DateTime d, CultureInfo cultureInfo)
        {
            if (cultureInfo.Name == "fi-FI")
                return d.ToString("yyyy.MM");
            else
                return d.ToString("yyyy-MM");
        }
    }
}
