namespace NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models
{
    public class BaseLoanExportRequestModel
    {
        public class Owner
        {
            public IdCodeType IdCodeType { get; set; }
            public string IdCode { get; set; }
        }

        public enum TargetEnvironment
        {
            Test,
            Production
        }

        public class LoanNumber
        {
            public LoanNumberType Type { get; set; }
            public string Number { get; set; }
        }

        public enum LoanNumberType
        {
            Iban,
            Bban,
            Other
        }

        public enum LoanType
        {
            LumpSumLoan,
        }

        public enum CurrencyCode
        {
            EUR,
            SEK,
            // Add other possible values if needed (ISO 4217 3-letter currency code)
        }

        public enum IdCodeType
        {
            PersonalIdentityCode,
            BusinessId,
            ForeignBusinessId
        }
    }
}
