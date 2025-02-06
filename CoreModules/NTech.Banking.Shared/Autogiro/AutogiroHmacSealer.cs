using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.Autogiro
{
    public class AutogiroHmacSealer
    {
        private readonly string key;
        private readonly Func<DateTime> now;

        public AutogiroHmacSealer(string key, Func<DateTime> now)
        {
            this.key = key;
            this.now = now;
        }

        public bool IsAlreadySealed(List<string> inputFile)
        {
            return inputFile != null && inputFile.Count > 1 && inputFile[0].StartsWith("00") && inputFile.Last().StartsWith("99");
        }

        public List<string> CreateSealedFile(List<string> inputFile)
        {
            if (IsAlreadySealed(inputFile))
                throw new Exception("File is already sealed");

            var sealDate = this.now();

            var tk00 = AutogiroRowBuilder
                .Start("00")
                .DateOnly(sealDate, twoDigitYearOnly: true)
                .String("HMAC", 4)
                .Space(68)
                .End();

            var keyBytes = ConvertHexStringToBytes(this.key);

            /*
            The KVV is calculated as a MAC for a "standard file" with the
            current key. (The "Standard file" for calculation of KVV has the contents:
            "00000000"             
             */
            var kvv = ComputeSeal(keyBytes, KvvStandardFileContent);

            var outputFile = new List<string>(inputFile.Count+2);
            outputFile.Add(tk00);
            outputFile.AddRange(inputFile);
            
            var seal = ComputeSeal(keyBytes, outputFile.ToArray());

            var tk99 = AutogiroRowBuilder
                .Start("99")
                .DateOnly(sealDate, twoDigitYearOnly: true)
                .String(kvv, 32) 
                .String(seal, 32)
                .Space(8)
                .End();

            outputFile.Add(tk99);

            return outputFile;
        }

        private const string KvvStandardFileContent = "00000000";

        //https://stackoverflow.com/questions/12185122/calculating-hmacsha256-using-c-sharp-to-match-payment-provider-example

        private static string ComputeSeal(byte[] key, params string[] lines)
        {
            //https://www.bankgirot.se/globalassets/dokument/tekniska-manualer/hmac_tamperprotection_technicalmanual_en.pdf
            /*
            If end-of-line characters are used in the file (LF, CRLF, Carriage Return Line
            Feed), these characters should not be included in the calculation             
             */
            var linesBytes = NormalizeAndConvertToBytes(string.Concat(lines));

            var hash = ComputeHash(key, linesBytes);

            /*
                Note that the MAC should be truncated to 128 bits since
                SH256 normally generates a 256-bit value. It is the first 128 bits that should be
                used as the MAC.
            */
            var truncatedHash = hash.Take(16).ToArray();
            return ConvertBytesToHexString(truncatedHash).ToUpperInvariant();
        }

        private static byte[] NormalizeAndConvertToBytes(string s)
        {
            /*
             * TODO: Normalize if we ever need text in these files
For the method pertaining to this document (HMAC-SHA256-128), all
characters in the 7-bit ASCII table (see the table at the end of this document),
excluding the control character, are included             
             */
            return Encoding.ASCII.GetBytes(s);
        }

        public static byte[] ComputeHash(byte[] key, byte[] message)
        {
            using (var hash = new HMACSHA256(key))
            {
                return hash.ComputeHash(message);
            }
        }
        
        public static string ConvertBytesToHexString(byte[] hash)
        {
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public static byte[] ConvertHexStringToBytes(string hex)
        {
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = byte.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber);
            }
            return bytes;
        }
    }
}
