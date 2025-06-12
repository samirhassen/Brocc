using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Core.Savings.Shared.Services.Utilities
{
    public interface IOcrNumber
    {
        string Country { get; }
        string NormalForm { get; }
        string DisplayForm { get; }
    }

    public class OcrNumberParser
    {
        private string country;

        public OcrNumberParser(string country)
        {
            this.country = country;
        }

        public IOcrNumber Parse(string ocr)
        {
            if (this.country == "SE")
                return OcrNumberSe.Parse(ocr);
            else if (this.country == "FI")
                return OcrNumberFi.Parse(ocr);
            else
                throw new NotImplementedException();
        }

        public bool TryParse(string ocr, out IOcrNumber parsed, out string errMsg)
        {
            parsed = null;
            if (this.country == "SE")
            {
                OcrNumberSe p;
                if (OcrNumberSe.TryParse(ocr, out p, out errMsg))
                {
                    parsed = p;
                    return true;
                }
                return false;
            }
            else if (this.country == "FI")
            {
                OcrNumberFi p;
                if (OcrNumberFi.TryParse(ocr, out p, out errMsg))
                {
                    parsed = p;
                    return true;
                }
                return false;
            }
            else
                throw new NotImplementedException();
        }
    }

    public class OcrNumberFi : IOcrNumber
    {
        private string ocr;

        public string Country
        {
            get
            {
                return "FI";
            }
        }

        public string NormalForm
        {
            get
            {
                return ocr;
            }
        }

        public string DisplayForm
        {
            get
            {
                //Group into groups of 5 from the right (123456789 -> 1234 56789) and remove all leading zeroes
                return string.Join(" ",
                    SplitIntoGroupsOfN(ocr.TrimStart('0')
                    .Reverse().ToArray(), 5)
                    .Select(x => new string(x.Reverse().ToArray()))
                    .Reverse());
            }
        }

        private static IEnumerable<IEnumerable<T>> SplitIntoGroupsOfN<T>(T[] array, int n)
        {
            for (var i = 0; i < (float)array.Length / n; i++)
            {
                yield return array.Skip(i * n).Take(n);
            }
        }

        public static OcrNumberFi FromSequenceNumber(long sequenceNr, int lengthWithoutCheckDigit)
        {
            if (sequenceNr <= 0) throw new ArgumentException("must be positive", "nr");

            var prefixString = sequenceNr.ToString().PadLeft(lengthWithoutCheckDigit, '0');
            var checkDigit = ComputeCheckDigit(prefixString);

            return new OcrNumberFi
            {
                ocr = $"{prefixString}{checkDigit.ToString()}"
            };
        }

        public static OcrNumberFi Parse(string ocr)
        {
            string msg;
            OcrNumberFi value;
            if (!TryParse(ocr, out value, out msg))
                throw new Exception(msg);
            else
                return value;
        }

        public static bool TryParse(string ocr, out OcrNumberFi parsed, out string errMsg)
        {
            ocr = ocr?.Replace(" ", "") ?? "";
            if (ocr.Length < 5)
            {
                errMsg = "Invalid length";
                parsed = null;
                return false;
            }

            if (ocr.Any(x => !Char.IsDigit(x)))
            {
                errMsg = "Invalid number";
                parsed = null;
                return false;
            }

            int cd;
            if (!int.TryParse(ocr.Last().ToString(), out cd))
            {
                errMsg = "Invalid number";
                parsed = null;
                return false;
            }

            if (cd != ComputeCheckDigit(ocr.Substring(0, ocr.Length - 1)))
            {
                parsed = null;
                errMsg = "Invalid checkdigit";
                return false;
            }

            errMsg = null;
            parsed = new OcrNumberFi
            {
                ocr = ocr
            };
            return true;
        }

        private static int ComputeCheckDigit(string prefixString)
        {
            Func<int, int> weight = i =>
            {
                switch (i % 3)
                {
                    case 0: return 7;
                    case 1: return 3;
                    default: return 1;
                }
            };

            var sum = prefixString.Reverse().Select((x, i) => int.Parse(x.ToString()) * weight(i)).Sum();
            var d = 10 - sum % 10;
            var checkDigit = d == 10 ? 0 : d;
            return checkDigit;
        }
    }

    public class OcrNumberSe : IOcrNumber
    {
        private string ocr;

        public string Country
        {
            get
            {
                return "SE";
            }
        }

        public string NormalForm
        {
            get
            {
                return ocr;
            }
        }

        public string DisplayForm
        {
            get
            {
                return ocr;
            }
        }

        public static OcrNumberSe FromSequenceNumber(long sequenceNr)
        {
            if (sequenceNr <= 0) throw new ArgumentException("must be positive", "nr");

            var prefix = sequenceNr.ToString();

            var lengthDigit = (prefix.Length + 2) % 10;
            var prefixString = prefix + lengthDigit.ToString();
            var checkDigit = ComputeMod10CheckDigit(prefixString);

            return new OcrNumberSe
            {
                ocr = $"{prefixString}{checkDigit.ToString()}"
            };
        }

        public static OcrNumberSe Parse(string ocr)
        {
            string msg;
            OcrNumberSe value;
            if (!TryParse(ocr, out value, out msg))
                throw new Exception(msg);
            else
                return value;
        }

        public static bool TryParse(string ocr, out OcrNumberSe parsed, out string errMsg)
        {
            ocr = ocr?.Replace(" ", "") ?? "";
            if (ocr.Length < 3)
            {
                errMsg = "Invalid length";
                parsed = null;
                return false;
            }

            int ld;
            if (!int.TryParse(ocr[ocr.Length - 2].ToString(), out ld))
            {
                errMsg = "Invalid number";
                parsed = null;
                return false;
            }

            int cd;
            if (!int.TryParse(ocr[ocr.Length - 1].ToString(), out cd))
            {
                errMsg = "Invalid number";
                parsed = null;
                return false;
            }

            if (ld != (ocr.Length % 10))
            {
                errMsg = "Invalid length";
                parsed = null;
                return false;
            }

            if (cd != ComputeMod10CheckDigit(ocr.Substring(0, ocr.Length - 1)))
            {
                errMsg = "Invalid checkdigit";
                parsed = null;
                return false;
            }

            parsed = new OcrNumberSe
            {
                ocr = ocr
            };
            errMsg = null;
            return true;
        }

        private static int ComputeMod10CheckDigit(string input)
        {
            return (10 - (input
                .Reverse()
                .Select((x, i) => (int.Parse(new string(new[] { x })) * (i % 2 == 0 ? 2 : 1)))
                .Sum(x => (x % 10) + (x >= 10 ? 1 : 0)) % 10)) % 10;
        }
    }
}