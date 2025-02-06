using System;
using System.Linq;

namespace NTech.Services.Infrastructure.NTechWs.Attributes
{
    public static class PaymentReferenceNumberValidator
    {
        public static bool IsValidPaymentReferenceNr(string bankAccountNrType, string paymentReferenceNr)
        {
            if (string.IsNullOrWhiteSpace(bankAccountNrType) || string.IsNullOrWhiteSpace(paymentReferenceNr))
                return false;

            var digitsAndWhitespace = paymentReferenceNr.Where(x => char.IsDigit(x) || char.IsWhiteSpace(x)).ToList();
            if (paymentReferenceNr.Length != digitsAndWhitespace.Count)
                return false;

            var cleanedNr = digitsAndWhitespace.Where(char.IsDigit).ToArray();

            if (bankAccountNrType == "BankGiroSe" || bankAccountNrType == "PlusGiroSe")
                return cleanedNr.Length >= 2 && cleanedNr.Length <= 25 && HasValidMod10CheckDigit(new string(cleanedNr));
            else
                throw new NotImplementedException(bankAccountNrType);
        }

        public static string NormalizePaymentReferenceNr(string bankAccountNrType, string paymentReferenceNr) =>
            IsValidPaymentReferenceNr(bankAccountNrType, paymentReferenceNr)
            ? new string(paymentReferenceNr.Where(char.IsDigit).ToArray())
            : paymentReferenceNr;

        private static bool HasValidMod10CheckDigit(string input)
        {
            var computedCheckDigit = (10 - (input
                .Take(input.Length - 1)
                .Reverse()
                .Select((x, i) => (int.Parse(new string(new[] { x })) * (i % 2 == 0 ? 2 : 1)))
                .Sum(x => (x % 10) + (x >= 10 ? 1 : 0)) % 10)) % 10;
            var actualCheckDigit = int.Parse(input.Substring(input.Length - 1));
            return computedCheckDigit == actualCheckDigit;
        }
    }
}
