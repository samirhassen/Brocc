using NTech.Banking.BankAccounts;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.BankAccounts.Se;
using NTech.Banking.CivicRegNumbers;
using System;
using System.Linq;

namespace nTest.RandomDataSource
{
    public class BankAccountGenerator
    {
        public IBankAccountNumber Generate(BankAccountNumberTypeCode code, IRandomnessSource random, Func<ICivicRegNumber> createCivicRegNr)
        {
            switch (code)
            {
                case BankAccountNumberTypeCode.IBANFi:
                    return GenerateIbanFi(random);
                case BankAccountNumberTypeCode.BankAccountSe:
                    return GenerateSwedishBankAccountNr(createCivicRegNr());
                case BankAccountNumberTypeCode.PlusGiroSe:
                    return GenerateSwedishPlusGiroNr(random);
                case BankAccountNumberTypeCode.BankGiroSe:
                    return GenerateSwedishBankGiroNr(random);
                default:
                    throw new NotImplementedException();
            }
        }

        public IBANFi GenerateIbanFi(IRandomnessSource random)
        {
            var i1 = random.NextIntBetween(1, 99999);
            var i2 = random.NextIntBetween(1, 999999);
            var nr = (i1.ToString() + i2.ToString()).PadLeft(11, '0');
            if (nr.Length != 11)
                throw new Exception();

            //FI1840551010234569 <-- Actual valid nr. The ones we create dont have a valid bban checkdigit only a valid iban checkdigit

            var bban = "405" + nr; //405 = aktia panki
            var checkDigits = ComputeCheckDigitsIbanFi(bban);
            return IBANFi.Parse($"FI{checkDigits}{bban}");
        }

        public IBankAccountNumber GenerateSwedishBankAccountNr(ICivicRegNumber c)
        {
            if (c.Country != "SE")
                throw new Exception();
            return BankAccountNumberSe.Parse($"3300{c.NormalizedValue.Substring(2)}"); //Nordea personkonto
        }

        public IBankAccountNumber GenerateSwedishBankGiroNr(IRandomnessSource random)
        {
            var prefix = "8" + random.NextIntBetween(1, 99999).ToString().PadLeft(5, '0');
            return BankGiroNumberSe.Parse($"{prefix}{ComputeMod10CheckDigit(prefix)}");
        }

        public IBankAccountNumber GenerateSwedishPlusGiroNr(IRandomnessSource random)
        {
            var prefix = "7" + random.NextIntBetween(1, 99999).ToString().PadLeft(5, '0');
            return PlusGiroNumberSe.Parse($"{prefix}{ComputeMod10CheckDigit(prefix)}");
        }

        private static string ComputeCheckDigitsIbanFi(string bban)
        {
            if (bban.Length != 14)
                throw new Exception("Finnish bban must be 14 chars");
            //15 = F, 18 = I
            var s = bban + "151800";
            var cd = 98 - BigModulo(s, 97);
            return cd.ToString().PadLeft(2, '0');
        }

        private static int BigModulo(string nr, int divisor)
        {
            //Source: http://www.devx.com/tips/Tip/39012
            const int PartLength = 9;
            while (nr.Length > PartLength)
            {
                var part = nr.Substring(0, PartLength);
                var mod = int.Parse(part) % divisor;
                nr = string.Format("{0}{1}", mod, nr.Substring(PartLength));
            }
            return int.Parse(nr) % divisor;
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