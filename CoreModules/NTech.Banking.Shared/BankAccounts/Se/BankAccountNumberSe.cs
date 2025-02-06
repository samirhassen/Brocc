using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace NTech.Banking.BankAccounts.Se
{
    /// <summary>
    /// Parses swedish bank account numbers described here:
    /// https://www.bankgirot.se/globalassets/dokument/anvandarmanualer/bankernaskontonummeruppbyggnad_anvandarmanual_sv.pdf
    ///
    /// Based on what this looked like at 2016-12-27.
    ///
    /// NOTE: We choose to ignore the fact that there are certain Swedbank account in the 8 range that dont have  a valid checkdigit but are still valid
    ///       since we would otherwise have to ignore validation for that range which seems worse than having a few old account that wont work.
    ///       Static whitelist is probably the way to go if one of these shows up.
    /// </summary>
    public class BankAccountNumberSe : IEquatable<BankAccountNumberSe>, IComparable<BankAccountNumberSe>, IBankAccountNumber
    {
        private readonly string accountNr;
        private readonly string clearingNr;
        private readonly string bankName;

        private class BankParsingRule
        {
            public Func<int, bool> HandlesClearingNumber { get; set; }
            public string BankName { get; set; }
            public string RuleSetName { get; set; }
        }

        private static XDocument LoadEmbeddedRulesFile()
        {
            return XDocuments.Parse(DefaultRulesFile);
        }

        private static Lazy<List<BankParsingRule>> BankParsingRules = new Lazy<List<BankParsingRule>>(() =>
        {
            Func<string, Func<int, bool>> createClearingMatcher = s =>
            {
                //Clearingrange is on the form x-y(,x-y)*
                var ranges = s
                    .Split(',')
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0)
                    .Select(x =>
                    {
                        var p = x.Split('-').Select(y => y.Trim()).Where(y => y.Length > 0).ToArray();
                        return Tuple.Create(int.Parse(p[0]), int.Parse(p[1]));
                    })
                    .ToList();
                return nr =>
                {
                    foreach (var range in ranges)
                    {
                        if (nr >= range.Item1 && nr <= range.Item2)
                            return true;
                    }
                    return false;
                };
            };

            var d = LoadEmbeddedRulesFile();
            var rules = new List<BankParsingRule>();
            foreach (var bankElement in d.Descendants().Where(x => x.Name == "Bank").ToList())
            {
                var name = bankElement.Elements().Single(x => x.Name.LocalName == "Name").Value;
                var clearingRange = bankElement.Elements().Single(x => x.Name.LocalName == "ClearingRange").Value;
                var ruleSet = bankElement.Elements().Single(x => x.Name.LocalName == "RuleSet").Value;

                rules.Add(new BankParsingRule
                {
                    BankName = name,
                    RuleSetName = ruleSet,
                    HandlesClearingNumber = createClearingMatcher(clearingRange)
                });
            }

            return rules;
        });

        private BankAccountNumberSe(string clearingNr, string accountNr, string bankName)
        {
            this.accountNr = accountNr;
            this.clearingNr = clearingNr;
            this.bankName = bankName;
        }

        public string BankName
        {
            get
            {
                return bankName;
            }
        }

        public string AccountNr
        {
            get
            {
                return accountNr;
            }
        }

        public string ClearingNr
        {
            get
            {
                return clearingNr;
            }
        }

        public string PaymentFileFormattedNr
        {
            get
            {
                //PaymentFileFormattedNr is padded with zeroes
                //as format is: [clearingNr]00000xxxxxxC
                return string.Format("{0}{1}", clearingNr, accountNr.PadLeft(12, '0'));
            }
        }

        public string NormalizedValue
        {
            get
            {
                return $"{clearingNr}{accountNr}";
            }
        }

        public string TwoLetterCountryIsoCode => "SE";

        public BankAccountNumberTypeCode AccountType => BankAccountNumberTypeCode.BankAccountSe;

        public static bool TryParse(string accountNr, out BankAccountNumberSe result, out string errorMessage)
        {
            var cleanedNr = new string((accountNr ?? "").Where(Char.IsDigit).ToArray());

            if (cleanedNr.Length < 5)
            {
                result = null;
                errorMessage = "Too short";
                return false;
            }

            var clearingPart = cleanedNr.Substring(0, 4);
            var accountPart = cleanedNr.Substring(4).TrimStart('0');

            foreach (var rule in BankParsingRules.Value)
            {
                if (rule.HandlesClearingNumber(int.Parse(clearingPart)))
                {
                    //Bank standard: CCCC - XX XXX XX
                    //(4 + 7 = 11 numbers)
                    switch(rule.RuleSetName)
                    {
                        case "Type1_1":
                            accountPart = accountPart.PadLeft(7, '0');
                            return TryParseTypeMod11WithCheckDigitIncludingClearingNr(accountPart, clearingPart, rule.BankName, true, out result, out errorMessage);
                        case "Type1_2":
                            accountPart = accountPart.PadLeft(7, '0');
                            return TryParseTypeMod11WithCheckDigitIncludingClearingNr(accountPart, clearingPart, rule.BankName, false, out result, out errorMessage);
                        case "Type2_1":
                            return TryParseFixedLengthMod10(cleanedNr.Substring(4), clearingPart, rule.BankName, out result, out errorMessage);
                        case "Type2_2":
                            return TryParseNineDigitMod11WithCheckDigitOnlyOnAccount(accountPart, clearingPart, rule.BankName, out result, out errorMessage);
                        case "Type2_3":
                            return TryParseVariableLengthMod10(accountPart, clearingPart, rule.BankName, out result, out errorMessage);
                        case "Type2_3_swedbank":
                            return TryParseVariableLengthMod10Swedbank(accountPart, clearingPart, rule.BankName, out result, out errorMessage);
                    }
                }
            }

            result = null;
            errorMessage = $"Not implemented for clearingNr: {clearingPart}";
            return false;
        }

        public static BankAccountNumberSe Parse(string accountNr)
        {
            BankAccountNumberSe result;
            string errorMessage;
            if (TryParse(accountNr, out result, out errorMessage))
                return result;
            else
                throw new Exception(errorMessage);
        }

        private static bool TryParseTypeMod11WithCheckDigitIncludingClearingNr(string accountPart, string clearingPart, string bankName, bool skipFirstClearingDigit, out BankAccountNumberSe result, out string errorMessage)
        {
            if (accountPart.Length != 7)
            {
                result = null;
                errorMessage = "Wrong length";
                return false;
            }

            if (!HasValidMod11CheckDigit(skipFirstClearingDigit ? (clearingPart + accountPart).Substring(1) : (clearingPart + accountPart)))
            {
                result = null;
                errorMessage = "Invalid checkdigit";
                return false;
            }

            result = new BankAccountNumberSe(clearingPart, accountPart, bankName);
            errorMessage = null;
            return true;
        }

        private enum Type2Type
        {
            Swedbank,
            TenDigitMod10,
            NineDigitMod11
        }

        private static string TrimAccountPart(string accountPart, string clearingNr)
        {
            if (clearingNr == "3300")
            {
                // Nordea personal account that have leading zeroes are corrupted by our standard normalization
                // so we exclude them here
                if (accountPart.Length == 10)
                    return accountPart;

                // Nordea personal account payment file numbers that have leading zeroes are corrupted by our standard normalization
                // so we remove trailing zeroes here
                else if (accountPart.Length == 12 && accountPart.StartsWith("00"))
                    return accountPart.Remove(0, 2); 
                else
                    return accountPart.TrimStart('0');
            }
            else
                return accountPart.TrimStart('0');
        }

        private static bool TryParseFixedLengthMod10(string accountPart, string clearingPart, string bankName, out BankAccountNumberSe result, out string errorMessage)
        {
            accountPart = TrimAccountPart(accountPart, clearingPart);

            if (accountPart.Length == 12 && clearingPart == "3300")
            {
                //Nordea personkonto will sometimes be written as <clearingnr 4><personnr 12>
                //which is technically not correct but common enough to handle
                var centuryPart = accountPart.Substring(0, 2);
                if (centuryPart != "19" && centuryPart != "20")
                {
                    result = null;
                    errorMessage = "Invalid personkonto";
                    return false;
                }
                accountPart = accountPart.Substring(2);
            }
            else if (accountPart.Length != 10)
            {
                result = null;
                errorMessage = "Wrong length";
                return false;
            }

            if (!IsValidMod10(accountPart))
            {
                result = null;
                errorMessage = "Invalid checkdigit";
                return false;
            }

            result = new BankAccountNumberSe(clearingPart, accountPart, bankName);
            errorMessage = null;
            return true;
        }

        private static bool TryParseVariableLengthMod10(string accountPart, string clearingPart, string bankName, out BankAccountNumberSe result, out string errorMessage)
        {
            if (accountPart.Length < 2 || accountPart.Length > 10)
            {
                result = null;
                errorMessage = "Wrong length";
                return false;
            }

            if (!IsValidMod10(accountPart))
            {
                result = null;
                errorMessage = "Invalid checkdigit";
                return false;
            }

            result = new BankAccountNumberSe(clearingPart, accountPart, bankName);
            errorMessage = null;
            return true;
        }

        private static bool TryParseVariableLengthMod10Swedbank(string accountPart, string clearingPart, string bankName, out BankAccountNumberSe result, out string errorMessage)
        {
            //Sparbankernas(Swedbank) standard: CCCC - C, XXX XXX XXX - X(5 + 10 = 15 siffror)
            if (accountPart.Length < 2 || accountPart.Length > 11)
            {
                result = null;
                errorMessage = "Wrong length";
                return false;
            }

            //The problem here is that the checkdigit might be 4-5 digits and the accountnr is also variable length up to 10 which
            //means the account structure is not well defined.
            var accountPartGuess1 = accountPart.Length > 10 ? accountPart.Substring(1) : accountPart;
            var accountPartGuess2 = accountPart.Substring(1);

            var isValid1 = false;
            var isValid2 = false;
            BankAccountNumberSe tempresult = null;
            if (IsValidMod10(accountPartGuess1))
            {
                isValid1 = true;
                tempresult = new BankAccountNumberSe(clearingPart, accountPartGuess1, bankName);
            }
            if (IsValidMod10(accountPartGuess2))
            {
                isValid2 = true;
                tempresult = new BankAccountNumberSe(clearingPart, accountPartGuess2, bankName);
            }

            if (!isValid1 && !isValid2)
            {
                result = null;
                errorMessage = "Invalid checkdigit";
                return false;
            }
            else if (isValid1 && isValid2 && accountPartGuess1 != accountPartGuess2)
            {
                result = null;
                errorMessage = "Ambigous swedbank account. Can't tell if it supposed to be clearing with checkdigits and 9 digits or clearing without checkdigt and 10 digits since both produce valid results.";
                return false;
            }
            else
            {
                result = tempresult;
                errorMessage = null;
                return true;
            }
        }

        private static bool TryParseNineDigitMod11WithCheckDigitOnlyOnAccount(string accountPart, string clearingPart, string bankName, out BankAccountNumberSe result, out string errorMessage)
        {
            accountPart = accountPart.TrimStart('0').PadLeft(9, '0');

            //Handelsbankens standard: CCCC - XXX XXX XXX (4 + 9 = 13 siffror)
            if (accountPart.Length != 9)
            {
                result = null;
                errorMessage = "Too long";
                return false;
            }

            if (!HasValidMod11CheckDigit(accountPart))
            {
                result = null;
                errorMessage = "Invalid checkdigit";
                return false;
            }

            result = new BankAccountNumberSe(clearingPart, accountPart, bankName);
            errorMessage = null;
            return true;
        }

        private static bool IsValidMod10(string input)
        {
            if (!(input != null && input.All(Char.IsDigit)))
                return false;

            if (input.Length < 2)
                return false;

            return ComputeMod10CheckDigit(input.Substring(0, input.Length - 1)).ToString() == input.Substring(input.Length - 1, 1);
        }

        private static int ComputeMod10CheckDigit(string input)
        {
            return (10 - (input
                .Reverse()
                .Select((x, i) => (int.Parse(new string(new[] { x })) * (i % 2 == 0 ? 2 : 1)))
                .Sum(x => (x % 10) + (x >= 10 ? 1 : 0)) % 10)) % 10;
        }

        private static bool HasValidMod11CheckDigit(string input)
        {
            var result = input
                .Reverse()
                .Select((x, i) => ((i % 10) + 1) * int.Parse(new string(new[] { x })))
                .Sum();
            return (result % 11) == 0;
        }

        public bool Equals(BankAccountNumberSe other)
        {
            if (other == null)
                return base.Equals(other);
            return this.PaymentFileFormattedNr.Equals(other.PaymentFileFormattedNr);
        }

        public int CompareTo(BankAccountNumberSe other)
        {
            if (other == null)
                return -1;
            else
                return this.PaymentFileFormattedNr.CompareTo(other.PaymentFileFormattedNr);
        }

        public override bool Equals(object other)
        {
            if (other == null)
                return false;

            BankAccountNumberSe o = other as BankAccountNumberSe;
            if (o == null)
                return false;
            else
                return Equals(o);
        }

        public override int GetHashCode()
        {
            return this.PaymentFileFormattedNr.GetHashCode();
        }

        public override string ToString()
        {
            return this.PaymentFileFormattedNr;
        }

        public string FormatFor(string formatName)
        {
            if (formatName == null)
                return this.NormalizedValue;
            else if (formatName.Equals("display", StringComparison.OrdinalIgnoreCase))
                return this.NormalizedValue;
            else if (formatName.Equals("bgmax", StringComparison.OrdinalIgnoreCase))
                return this.PaymentFileFormattedNr;
            else if (formatName.Equals("pain.001.001.3", StringComparison.OrdinalIgnoreCase))
                return $"{this.clearingNr}{this.accountNr}";
            else
                throw new NotImplementedException();
        }

        public const string DefaultRulesFile = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Banks>
	<Bank>
		<Name>Svea Bank</Name>
		<ClearingRange>9660-9669</ClearingRange>
		<RuleSet>Type1_2</RuleSet>
	</Bank>
	<Bank>
		<Name>Avanza Bank AB</Name>
		<ClearingRange>9550-9569</ClearingRange>
		<RuleSet>Type1_2</RuleSet>
	</Bank>
	<Bank>
		<Name>BlueStep Finans AB</Name>
		<ClearingRange>9680-9689</ClearingRange>
		<RuleSet>Type1_1</RuleSet>
	</Bank>
	<Bank>
		<Name>BNP Paribas Fortis Bank</Name>
		<ClearingRange>9470-9479</ClearingRange>
		<RuleSet>Type1_2</RuleSet>
	</Bank>
	<Bank>
		<Name>Citibank</Name>
		<ClearingRange>9040-9049</ClearingRange>
		<RuleSet>Type1_2</RuleSet>
	</Bank>
	<Bank>
		<Name>Danske Bank</Name>
		<ClearingRange>1200-1399,2400-2499</ClearingRange>
		<RuleSet>Type1_1</RuleSet>
	</Bank>
	<Bank>
		<Name>DNB Bank </Name>
		<ClearingRange>9190-9199,9260-9269</ClearingRange>
		<RuleSet>Type1_2</RuleSet>
	</Bank>
	<Bank>
		<Name>Ekobanken</Name>
		<ClearingRange>9700-9709</ClearingRange>
		<RuleSet>Type1_2</RuleSet>
	</Bank>
	<Bank>
		<Name>Erik Penser AB</Name>
		<ClearingRange>9590-9599</ClearingRange>
		<RuleSet>Type1_2</RuleSet>
	</Bank>
	<Bank>
		<Name>Forex Bank</Name>
		<ClearingRange>9400-9449</ClearingRange>
		<RuleSet>Type1_1</RuleSet>
	</Bank>
	<Bank>
		<Name>ICA Banken AB</Name>
		<ClearingRange>9270-9279</ClearingRange>
		<RuleSet>Type1_1</RuleSet>
	</Bank>
	<Bank>
		<Name>JAK Medlemsbank</Name>
		<ClearingRange>9670-9679</ClearingRange>
		<RuleSet>Type1_2</RuleSet>
	</Bank>
	<Bank>
		<Name>Landshypotek AB</Name>
		<ClearingRange>9390-9399</ClearingRange>
		<RuleSet>Type1_2</RuleSet>
	</Bank>
	<Bank>
		<Name>Lån och Spar Bank Sverige</Name>
		<ClearingRange>9630-9639</ClearingRange>
		<RuleSet>Type1_1</RuleSet>
	</Bank>
	<Bank>
		<Name>Länsförsäkringar Bank</Name>
		<ClearingRange>3400-3409,9060-9069</ClearingRange>
		<RuleSet>Type1_1</RuleSet>
	</Bank>
	<Bank>
		<Name>Länsförsäkringar Bank</Name>
		<ClearingRange>9020-9029</ClearingRange>
		<RuleSet>Type1_2</RuleSet>
	</Bank>
	<Bank>
		<Name>Marginalen Bank</Name>
		<ClearingRange>9230-9239</ClearingRange>
		<RuleSet>Type1_1</RuleSet>
	</Bank>
	<Bank>
		<Name>Nordax Bank AB</Name>
		<ClearingRange>9640-9649</ClearingRange>
		<RuleSet>Type1_2</RuleSet>
	</Bank>
	<Bank>
		<Name>Nordea</Name>
		<ClearingRange>1100-1199,1400-2099,3000-3299,3301-3399,3410-3781,3783-3999</ClearingRange>
		<RuleSet>Type1_1</RuleSet>
	</Bank>
	<Bank>
		<Name>Nordea</Name>
		<ClearingRange>4000-4999</ClearingRange>
		<RuleSet>Type1_2</RuleSet>
	</Bank>
	<Bank>
		<Name>Nordnet Bank</Name>
		<ClearingRange>9100-9109</ClearingRange>
		<RuleSet>Type1_2</RuleSet>
	</Bank>
	<Bank>
		<Name>Resurs Bank</Name>
		<ClearingRange>9280-9289</ClearingRange>
		<RuleSet>Type1_1</RuleSet>
	</Bank>
	<Bank>
		<Name>Riksgälden</Name>
		<ClearingRange>9880-9889</ClearingRange>
		<RuleSet>Type1_2</RuleSet>
	</Bank>
	<Bank>
		<Name>Royal bank of Scotland</Name>
		<ClearingRange>9090-9099</ClearingRange>
		<RuleSet>Type1_2</RuleSet>
	</Bank>
	<Bank>
		<Name>Santander Consumer Bank AS</Name>
		<ClearingRange>9460-9469</ClearingRange>
		<RuleSet>Type1_1</RuleSet>
	</Bank>
	<Bank>
		<Name>SBAB</Name>
		<ClearingRange>9250-9259</ClearingRange>
		<RuleSet>Type1_1</RuleSet>
	</Bank>
	<Bank>
		<Name>SEB</Name>
		<ClearingRange>5000-5999,9120-9124,9130-9149</ClearingRange>
		<RuleSet>Type1_1</RuleSet>
	</Bank>
	<Bank>
		<Name>Skandiabanken</Name>
		<ClearingRange>9150-9169</ClearingRange>
		<RuleSet>Type1_2</RuleSet>
	</Bank>
	<Bank>
		<Name>Swedbank</Name>
		<ClearingRange>7000-7999</ClearingRange>
		<RuleSet>Type1_1</RuleSet>
	</Bank>
	<Bank>
		<Name>Ålandsbanken Sverige AB</Name>
		<ClearingRange>2300-2399</ClearingRange>
		<RuleSet>Type1_2</RuleSet>
	</Bank>
	<Bank>
		<Name>Danske Bank</Name>
		<ClearingRange>9180-9189</ClearingRange>
		<RuleSet>Type2_1</RuleSet>
	</Bank>
	<Bank>
		<Name>Handelsbanken</Name>
		<ClearingRange>6000-6999</ClearingRange>
		<RuleSet>Type2_2</RuleSet>
	</Bank>
	<Bank>
		<Name>Nordea/Plusgirot</Name>
		<ClearingRange>9500-9549,9960-9969</ClearingRange>
		<RuleSet>Type2_3</RuleSet>
	</Bank>
	<Bank>
		<Name>Nordea</Name>
		<ClearingRange>3300-3300,3782-3782</ClearingRange>
		<RuleSet>Type2_1</RuleSet>
	</Bank>
	<Bank>
		<Name>Riksgälden</Name>
		<ClearingRange>9890-9899</ClearingRange>
		<RuleSet>Type2_1</RuleSet>
	</Bank>
	<Bank>
		<Name>Sparbanken Syd</Name>
		<ClearingRange>9570-9579</ClearingRange>
		<RuleSet>Type2_1</RuleSet>
	</Bank>
	<Bank>
		<Name>Swedbank</Name>
		<ClearingRange>8000-8999</ClearingRange>
		<RuleSet>Type2_3_swedbank</RuleSet>
	</Bank>
	<Bank>
		<Name>Swedbank (f.d.Sparbanken Öresund)</Name>
		<ClearingRange>9300-9329,9330-9349</ClearingRange>
		<RuleSet>Type2_1</RuleSet>
	</Bank>
</Banks>	";
    }
}