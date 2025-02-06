using System;
using System.Collections.Concurrent;

namespace nCustomer
{
    public class PhoneNumberHandler
    {
        private readonly string regionCode;
        private readonly Lazy<PhoneNumbers.PhoneNumberUtil> phoneUtil = new Lazy<PhoneNumbers.PhoneNumberUtil>(() => PhoneNumbers.PhoneNumberUtil.GetInstance());

        private static readonly ConcurrentDictionary<string, PhoneNumberHandler> handlers = new ConcurrentDictionary<string, PhoneNumberHandler>(StringComparer.OrdinalIgnoreCase);

        /// <param name="regionCode">Two letter iso like SE for sweden</param>
        public static PhoneNumberHandler GetInstance(string regionCode)
        {
            return handlers.GetOrAdd(regionCode, rc => new PhoneNumberHandler(rc));
        }

        private PhoneNumberHandler(string regionCode)
        {
            this.regionCode = regionCode;
        }

        public class ParseResult
        {
            public string Raw { get; internal set; }
            public bool IsValid { get; set; }
            public ValidNumberResult ValidNumber { get; set; }
            public string ErrorCode { get; set; }
            public Exception ErrorException { get; set; }

            public class ValidNumberResult
            {
                public bool IsLocal { get; set; }
                public string NumberType { get; set; }
                public string RegionCode { get; set; }
                public string LocalNumber { get; set; }
                public string InternationalNumber { get; set; }
                public string MobileDialingNumber { get; set; }
            }
        }

        public string TryNormalizeToInternationalFormat(string nr, Action<ParseResult> observeResult = null)
        {
            if (string.IsNullOrWhiteSpace(nr))
                return nr;
            var result = Parse(nr);
            observeResult?.Invoke(result);
            return result.IsValid ? result.ValidNumber.InternationalNumber : nr;
        }

        public ParseResult Parse(string rawNr)
        {
            var result = new ParseResult
            {
                Raw = rawNr,
                IsValid = false
            };
            try
            {
                var number = phoneUtil.Value.ParseAndKeepRawInput(rawNr, regionCode);

                var isPossible = phoneUtil.Value.IsPossibleNumber(number);
                if (!isPossible)
                {
                    switch (phoneUtil.Value.IsPossibleNumberWithReason(number))
                    {
                        case PhoneNumbers.PhoneNumberUtil.ValidationResult.INVALID_COUNTRY_CODE:
                            result.ErrorCode = "INVALID_COUNTRY_CODE";
                            break;
                        case PhoneNumbers.PhoneNumberUtil.ValidationResult.TOO_SHORT:
                            result.ErrorCode = "TOO_SHORT";
                            break;
                        case PhoneNumbers.PhoneNumberUtil.ValidationResult.TOO_LONG:
                            result.ErrorCode = "TOO_LONG";
                            break;
                        default:
                            result.ErrorCode = "OTHER";
                            break;
                    }
                    return result;
                }
                else
                {
                    var isNumberValid = phoneUtil.Value.IsValidNumber(number);
                    if (!isNumberValid)
                    {
                        result.ErrorCode = "INVALID";
                        return result;
                    }
                    result.IsValid = true;
                    result.ValidNumber = new ParseResult.ValidNumberResult
                    {
                        IsLocal = false
                    };
                    if (isNumberValid && !string.IsNullOrWhiteSpace(regionCode) && regionCode != "ZZ")
                    {
                        result.ValidNumber.IsLocal = phoneUtil.Value.IsValidNumberForRegion(number, regionCode);
                    }
                    result.ValidNumber.RegionCode = phoneUtil.Value.GetRegionCodeForNumber(number);
                    result.ValidNumber.NumberType = phoneUtil.Value.GetNumberType(number).ToString();
                    result.ValidNumber.LocalNumber = phoneUtil.Value.Format(number, PhoneNumbers.PhoneNumberFormat.NATIONAL);
                    result.ValidNumber.InternationalNumber = phoneUtil.Value.Format(number, PhoneNumbers.PhoneNumberFormat.E164);
                    result.ValidNumber.MobileDialingNumber = phoneUtil.Value.FormatNumberForMobileDialing(number, regionCode, false);
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.ErrorCode = "EXCEPTION";
                result.ErrorException = ex;
                return result;
            }
        }
    }
}