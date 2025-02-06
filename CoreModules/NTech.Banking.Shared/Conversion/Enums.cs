using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.Conversion
{
    public static class Enums
    {
        public static IList<T> GetAllValues<T>() where T : struct
        {
            return Enum.GetValues(typeof(T)).Cast<T>().ToList();
        }

        public static T? Parse<T>(string value, bool ignoreCase = false) where T : struct
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            T v;
            if (ignoreCase) //The documentation for Enum.TryParse doesnt specify what the default is for ignoreCase so splitting it up to be safe.
            {
                if (Enum.TryParse<T>(value, true, out v))
                    return v;
                else
                    return null;
            }
            else
            {
                if (Enum.TryParse<T>(value, out v))
                    return v;
                else
                    return null;
            }
        }

        public static T ParseReq<T>(string value) where T : struct
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException("value", "value cannot be null");

            T v;
            if (Enum.TryParse<T>(value, out v))
                return v;
            else
                throw new Exception("Invalid enum value");
        }
    }
}
