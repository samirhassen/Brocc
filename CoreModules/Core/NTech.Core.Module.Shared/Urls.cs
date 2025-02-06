using System;
using System.Collections.Generic;

namespace NTech.Services.Infrastructure
{
    public static class Urls
    {
        //Based on https://github.com/neosmart/UrlBase64/blob/master/UrlBase64/UrlBase64.cs

        private static readonly char[] TwoPads = { '=', '=' };

        public static string ToUrlSafeBase64String(byte[] bytes)
        {
            var encoded = Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_');
            encoded = encoded.TrimEnd('=');
            return encoded;
        }

        public static byte[] FromUrlSafeBase64String(string encoded)
        {
            var chars = new List<char>(encoded.ToCharArray());

            for (int i = 0; i < chars.Count; ++i)
            {
                if (chars[i] == '_')
                {
                    chars[i] = '/';
                }
                else if (chars[i] == '-')
                {
                    chars[i] = '+';
                }
            }

            switch (encoded.Length % 4)
            {
                case 2:
                    chars.AddRange(TwoPads);
                    break;

                case 3:
                    chars.Add('=');
                    break;
            }

            var array = chars.ToArray();

            return Convert.FromBase64CharArray(array, 0, array.Length);
        }
    }
}