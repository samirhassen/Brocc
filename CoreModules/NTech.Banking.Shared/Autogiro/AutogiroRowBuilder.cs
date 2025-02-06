using System;
using System.Globalization;

namespace NTech.Banking.Autogiro
{
    public class AutogiroRowBuilder
    {
        private string row = "";
        private readonly Action<string> onEnd;

        private AutogiroRowBuilder(string postType, Action<string> onEnd)
        {
            this.onEnd = onEnd;
            String(postType, 2);
        }

        public static AutogiroRowBuilder Start(string postType, Action<string> onEnd = null)
        {
            return new AutogiroRowBuilder(postType, onEnd);
        }

        public AutogiroRowBuilder Space(int length)
        {
            row += new string(' ', length);
            return this;
        }

        public AutogiroRowBuilder DateOnly(DateTime d, bool twoDigitYearOnly = false)
        {
            row += d.ToString(twoDigitYearOnly ? "yyMMdd" : "yyyyMMdd");
            return this;
        }
            
        public AutogiroRowBuilder String(string s, int length, bool rightAligned = false, char paddingChar = ' ')
        {
            s = (s ?? "").Trim();
            if (s.Length > length)
                throw new Exception("string is to long");
            row += rightAligned ? s.PadLeft(length, paddingChar) : s.PadRight(length, paddingChar);

            return this;
        }

        public AutogiroRowBuilder Money(decimal d, int length)
        {
            if (d < 0)
                throw new Exception("Cannot be negative");
            var s = d.ToString("f2", CultureInfo.InvariantCulture).Replace(".", "");
            if (s.Length > length)
                throw new Exception("Max length is " + length);
            row += s.PadLeft(length, '0');

            return this;
        }

        public string End()
        {
            if (row.Length != 80)
                throw new Exception("Row length must be 80");
            onEnd?.Invoke(row);
            return row;
        }
    } 
}
