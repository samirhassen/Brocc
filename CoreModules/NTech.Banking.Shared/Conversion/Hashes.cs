using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.Conversion
{
    public static class Hashes
    {
        public static string Sha256(string s)
        {
            using (var crypt = new System.Security.Cryptography.SHA256Managed())
            {
                var hash = new System.Text.StringBuilder();
                byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(s));
                foreach (byte theByte in crypto)
                {
                    hash.Append(theByte.ToString("x2"));
                }
                return hash.ToString();
            }            
        }

        public static string Md5(params string[] input)
        {
            var s = string.Concat(input.Select(x => x?.Trim()?.ToLowerInvariant()));
            using (var md5Hash = System.Security.Cryptography.MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(s));
                return Convert.ToBase64String(data);
            }
        }
    }
}
