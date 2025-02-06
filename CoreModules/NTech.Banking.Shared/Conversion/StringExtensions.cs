using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public static class StringExtensions
    {
        public static bool IsOneOf(this string source, params string[] args)
        {
            foreach (var a in args)
            {
                if (a == source)
                    return true;
            }
            return false;
        }

        public static bool IsOneOfIgnoreCase(this string source, params string[] args)
        {
            foreach (var a in args)
            {
                if (a.EqualsIgnoreCase(source))
                    return true;
            }
            return false;
        }

        public static bool EqualsIgnoreCase(this string source, string otherString)
        {
            if (source == null)
                return otherString == null;
            else
                return source.Equals(otherString, StringComparison.OrdinalIgnoreCase);
        }
    }
}
