using System;
using System.Globalization;

namespace nPreCredit
{
    public class StringItem
    {
        private readonly string value;
        private readonly string context;
        private readonly Action<string> set;
        private readonly Tuple<DateTimeOffset?, int?> changedWhenAndBy;

        public StringItem(string value, string context, Action<string> set, Tuple<DateTimeOffset?, int?> changedWhenAndBy)
        {
            this.value = value;
            this.context = context;
            this.set = set;
            this.changedWhenAndBy = changedWhenAndBy;
        }

        private string RawStringValue
        {
            get
            {
                return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }
        }

        public bool Exists
        {
            get
            {
                return !string.IsNullOrWhiteSpace(this.value);
            }
        }

        public int? ChangedByUserId
        {
            get
            {
                if (this.changedWhenAndBy == null)
                    throw new Exception("Change information was not loaded");
                return this.changedWhenAndBy.Item2;
            }
        }

        public DateTimeOffset? ChangedDate
        {
            get
            {
                if (this.changedWhenAndBy == null)
                    throw new Exception("Change information was not loaded");
                return this.changedWhenAndBy.Item1;
            }
        }

        public bool WasChangeInformationLoaded
        {
            get
            {
                return this.changedWhenAndBy != null;
            }
        }

        public RequireableClass<string> StringValue
        {
            get
            {
                return new RequireableClass<string>(RawStringValue, context);
            }
        }

        public RequireableStruct<int> IntValue
        {
            get
            {
                var v = RawStringValue;
                if (v == null)
                    return new RequireableStruct<int>(null, context);

                int d;
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out d))
                    return new RequireableStruct<int>(d, context);
                else
                    throw new Exception($"{context}: Invalid int");
            }
        }

        public RequireableStruct<decimal> DecimalValue
        {
            get
            {
                var v = RawStringValue;
                if (v == null)
                    return new RequireableStruct<decimal>(null, context);

                var d = ParseDecimal(v);
                if (d.HasValue)
                    return new RequireableStruct<decimal>(d.Value, context);
                else
                    throw new Exception($"{context}: Invalid decimal");
            }
        }

        public static decimal? ParseDecimal(string v)
        {
            decimal d;
            if (string.IsNullOrWhiteSpace(v))
                return null;
            if (decimal.TryParse(v, NumberStyles.Integer | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out d))
                return d;
            else
                return null;
        }

        public static int? ParseInt(string v)
        {
            int d;
            if (string.IsNullOrWhiteSpace(v))
                return null;
            if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out d))
                return d;
            else
                return null;
        }

        public static bool? ParseBool(string v)
        {
            if (string.IsNullOrWhiteSpace(v))
                return null;
            return (v ?? "").ToLowerInvariant() == "true";
        }

        public RequireableStruct<DateTime> MonthValue(bool useNullIfInvalid)
        {

            var v = RawStringValue;
            if (v == null)
                return new RequireableStruct<DateTime>(null, context);

            if (v.Length == 7) //standard format is yyyy-mm but someone may accidently set it to yyyy-mm-01 to begin with
                v = v + "-01";

            DateTime m;
            if (DateTime.TryParseExact(v, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out m))
                return new RequireableStruct<DateTime>(m, context);
            else if (useNullIfInvalid)
            {
                return new RequireableStruct<DateTime>(null, context);
            }
            else
                throw new Exception($"{context}: Invalid month");
        }

        public RequireableStruct<DateTimeOffset> DateValue
        {
            get
            {
                var v = RawStringValue;
                if (v == null)
                    return new RequireableStruct<DateTimeOffset>(null, context);

                var s = v;
                if (v.Length > 10)
                    s = s.Substring(0, 10);

                DateTime d;
                if (DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                    return new RequireableStruct<DateTimeOffset>(d, context);
                else
                    throw new Exception($"{context}: Invalid date");
            }
        }

        public RequireableStruct<DateTimeOffset> DateAndTimeValue
        {
            get
            {
                var v = RawStringValue;
                if (v == null)
                    return new RequireableStruct<DateTimeOffset>(null, context);

                var s = v;

                DateTime d;
                if (DateTime.TryParseExact(s, "o", CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                    return new RequireableStruct<DateTimeOffset>(d, context);
                else
                    throw new Exception($"{context}: Invalid date and time");
            }
        }

        public RequireableStruct<bool> BoolValue
        {
            get
            {
                var v = RawStringValue;
                if (v == null)
                    return new RequireableStruct<bool>(null, context);

                v = v.ToLowerInvariant();
                return new RequireableStruct<bool>(v == "true" || v == "1" || v == "yes", context);
            }
        }

        public void Set(string value)
        {
            set(string.IsNullOrWhiteSpace(value) ? null : value);
        }

        public void Set(bool? value)
        {
            if (!value.HasValue)
                set(null);

            set(value.Value ? "true" : "false");
        }

        public void Set(decimal? value)
        {
            if (!value.HasValue)
                set(null);

            set(value.Value.ToString(CultureInfo.InvariantCulture));
        }

        public void Set(DateTimeOffset? value, bool includeTime = false)
        {
            if (!value.HasValue)
                set(null);

            if (includeTime)
                set(value.Value.ToString("o"));
            else
                set(value.Value.ToString("yyyy-MM-dd"));
        }
    }
}
