using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.BankAccounts.Se;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.BankAccounts
{
    public class BankAccountNumberParser
    {
        private readonly string countryCode;

        public BankAccountNumberParser(string countryCode)
        {
            this.countryCode = countryCode;
        }

        public bool TryParseBankAccount(string nr, BankAccountNumberTypeCode typeCode, out IBankAccountNumber result)
        {
            if (countryCode == "FI")
            {
                switch (typeCode)
                {
                    case BankAccountNumberTypeCode.IBANFi:
                        return TryParseCast<IBANFi>(IBANFi.TryParse, nr, typeCode, out result);
                    default:
                        throw new Exception($"FI: {typeCode} not supported");
                }
            }
            else if (countryCode == "SE")
            {
                switch (typeCode)
                {
                    case BankAccountNumberTypeCode.BankAccountSe:
                        return TryParseCast(SwallowMessage<BankAccountNumberSe>(BankAccountNumberSe.TryParse), nr, typeCode, out result);
                    case BankAccountNumberTypeCode.BankGiroSe:
                        return TryParseCast<BankGiroNumberSe>(BankGiroNumberSe.TryParse, nr, typeCode, out result);
                    case BankAccountNumberTypeCode.PlusGiroSe:
                        return TryParseCast<PlusGiroNumberSe>(PlusGiroNumberSe.TryParse, nr, typeCode, out result);
                    default:
                        throw new Exception($"SE: {typeCode} not supported");
                }
            }
            else
                throw new Exception($"{countryCode}: {typeCode} not supported");
        }

        public IBankAccountNumber ParseBankAccount(string nr, BankAccountNumberTypeCode typeCode)
        {
            IBankAccountNumber b;
            if (TryParseBankAccount(nr, typeCode, out b))
                return b;
            throw new Exception("Invalid account nr");
        }

        public bool TryParseFromStringWithDefaults(string nr, string typeCode, out IBankAccountNumber result)
        {
            if (string.IsNullOrWhiteSpace(typeCode))
                typeCode = GetDefaultAccountTypeByCountryCode(countryCode).ToString();

            BankAccountNumberTypeCode tc;
            if (!Enum.TryParse(typeCode, out tc))
                throw new Exception($"Invalid typeCode '{typeCode}'");

            result = null;

            return TryParseBankAccount(nr, tc, out result);
        }

        public IBankAccountNumber ParseFromStringWithDefaults(string nr,  string typeCode)
        {
            if (string.IsNullOrWhiteSpace(typeCode))
                typeCode = GetDefaultAccountTypeByCountryCode(countryCode).ToString();

            BankAccountNumberTypeCode tc;
            if (!Enum.TryParse(typeCode, out tc))
                throw new Exception($"Invalid typeCode '{typeCode}'");

            return ParseBankAccount(nr, tc);
        }

        public static BankAccountNumberTypeCode GetDefaultAccountTypeByCountryCode(string countryCode)
        {
            if (countryCode == "SE")
                return BankAccountNumberTypeCode.BankAccountSe;

            if (countryCode == "FI")
                return BankAccountNumberTypeCode.IBANFi;

            throw new NotImplementedException();
        }

        private delegate bool TryParseAccount<T>(string nr, out T result);
        private delegate bool TryParseAccountM<T>(string nr, out T result, out string message);
        private TryParseAccount<T> SwallowMessage<T>(TryParseAccountM<T> t)
        {
            string m;
            TryParseAccount<T> f = (string x, out T y) => t(x, out y, out m);
            return f;
        }

        private bool TryParseCast<TAccount>(TryParseAccount<TAccount> f, string nr, BankAccountNumberTypeCode typeCode, out IBankAccountNumber result) where TAccount : IBankAccountNumber
        {
            TAccount r;
            if (f(nr, out r))
            {
                result = r;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }
    }
}
