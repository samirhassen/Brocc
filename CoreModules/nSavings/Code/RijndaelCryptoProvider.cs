using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace nSavings.Code
{
    public class RijndaelCryptoProvider
    {
        private static readonly Encoding Encoding = Encoding.UTF8;

        private static void InitCrypto(Rijndael rj)
        {
            rj.Padding = PaddingMode.Zeros;
            rj.BlockSize = 256;
            rj.Mode = CipherMode.CBC;
        }

        public static string GenerateIv()
        {
            using (var rj = RijndaelManaged.Create())
            {
                InitCrypto(rj);
                rj.GenerateIV();
                return Convert.ToBase64String(rj.IV);
            }
        }

        public static string GenerateKey()
        {
            using (var rj = RijndaelManaged.Create())
            {
                InitCrypto(rj);
                rj.GenerateKey();
                return Convert.ToBase64String(rj.Key);
            }
        }

        public static string Encrypt(string iv, string key, string plaintextMessage)
        {
            var messageBytes = Encoding.GetBytes(plaintextMessage);
            using (var rj = RijndaelManaged.Create())
            {
                InitCrypto(rj);

                rj.IV = Convert.FromBase64String(iv);
                rj.Key = Convert.FromBase64String(key);

                using (var c = rj.CreateEncryptor())
                using (var rs = new MemoryStream())
                {
                    using (var s = new CryptoStream(rs, c, CryptoStreamMode.Write))
                    using (var w = new BinaryWriter(s))
                    {
                        w.Write(messageBytes, 0, messageBytes.Length);
                    }

                    return Convert.ToBase64String(rs.ToArray());
                }
            }
        }

        public static string Decrypt(string iv, string key, string encryptedMessage)
        {
            var messageBytes = Convert.FromBase64String(encryptedMessage);
            using (var rj = RijndaelManaged.Create())
            {
                InitCrypto(rj);

                rj.IV = Convert.FromBase64String(iv);
                rj.Key = Convert.FromBase64String(key);

                using (var c = rj.CreateDecryptor())
                using (var rs = new MemoryStream())
                {
                    using (var s = new CryptoStream(rs, c, CryptoStreamMode.Write))
                    using (var w = new BinaryWriter(s))
                    {
                        w.Write(messageBytes, 0, messageBytes.Length);
                    }

                    return rj.Padding == PaddingMode.Zeros
                        ? Encoding.UTF8.GetString(rs.ToArray()).TrimEnd('\0')
                        : Encoding.UTF8.GetString(rs.ToArray());
                }
            }
        }
    }
}