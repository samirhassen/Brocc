using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.Autogiro
{
    public class AutogiroPaymentNumberGenerator
    {
        public string GenerateNr(string creditNr, int applicantNr)
        {
            //<ett><lånenr utom bokstäver med ledande nollor till 9 i längd><sökandenr><checksiffra mod 10>

            if (string.IsNullOrWhiteSpace(creditNr))
                throw new Exception("Missing creditNr");

            var creditNrPart = new string(creditNr.Where(Char.IsDigit).ToArray());

            if (string.IsNullOrWhiteSpace(creditNrPart) || creditNrPart.Length > 9)
                throw new Exception("Invalid creditNr");

            if (applicantNr < 1 || applicantNr > 9)
                throw new Exception("Applicantnr must be 1-9");

            var prefix = $"1{creditNrPart.PadLeft(9, '0')}{applicantNr.ToString()}";

            return $"{prefix}{ComputeMod10CheckDigit(prefix)}";
        }

        //Basically run GenerateNr backwards.
        public bool TryExtractPartialCreditNrAndApplicantNrFromPaymentNr(string paymentNr, out string partialCreditNr, out int? applicantNr)
        {
            partialCreditNr = null;
            applicantNr = null;

            var p = (paymentNr ?? "").Trim();
            if (p.Length != 12)
                return false;

            if (p[0] != '1')
                return false;

            if (!char.IsNumber(p[10]))
                return false;

            if (ComputeMod10CheckDigit(p.Substring(0, 11)).ToString()[0] != p.Last())
                return false;

            var pn = p.Substring(1, 9).TrimStart('0');
            if (pn.Length == 0)
                return false;

            partialCreditNr = pn;
            applicantNr = int.Parse(new string(new[] { p[10] }));

            return true;
        }

        public int? ExtractApplicantNrOrNull(string paymentNr, string creditNr)
        {
            if (GenerateNr(creditNr, 1) == paymentNr)
                return 1;
            if (GenerateNr(creditNr, 2) == paymentNr)
                return 2;
            return null;
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
