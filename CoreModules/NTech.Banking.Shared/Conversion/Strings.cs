using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.Conversion
{
    public static class Strings
    {
        /// <summary>
        /// MaskString("xx-xx-xx", "654321") -> "65-43-21"
        /// MaskString("xx-xx-xx", "87654321") -> "8765-43-21"
        /// MaskString("00-xx+x", "87654321") -> "876-54-32+1"
        /// 
        /// x can be any letter or digit. Other chars are inserted as is. (Like the '-' or + in the example)
        /// </summary>
        public static string MaskString(string mask, string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var maskPosition = 0;
            var output = "";
            var maskRev = mask.Reverse().ToArray();
            foreach (var c in input.Reverse())
            {
                while (maskPosition < maskRev.Length && !Char.IsLetterOrDigit(maskRev[maskPosition]))
                {
                    output += maskRev[maskPosition].ToString();
                    maskPosition++;
                }
                maskPosition++;
                output += c.ToString();
            }

            return new string(output.Reverse().ToArray());
        }
    }
}
