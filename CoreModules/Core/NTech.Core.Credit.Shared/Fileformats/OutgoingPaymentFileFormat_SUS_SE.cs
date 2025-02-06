using NTech.Banking.BankAccounts;
using NTech.Banking.BankAccounts.Se;
using NTech.Banking.OrganisationNumbers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace nCredit.Code.Fileformats
{
    /// <summary>
    /// A specific format for Swedbank "Swedbank Utbetalnings System".
    /// Defined in a file called SUS_teknisk_info.pdf
    /// Tech support: 08-585 940 07 / 950kst@swedbank.se
    /// </summary>
    public class OutgoingPaymentFileFormat_SUS_SE
    {
        private SwedbankSeSettings Settings { get; set; }
        private DateTime FileCreationDate { get; set; }

        public OutgoingPaymentFileFormat_SUS_SE(bool isProduction, SwedbankSeSettings settings, DateTime creationDate)
        {
            Settings = settings;
            FileCreationDate = !isProduction
                ? creationDate.AddMonths(1) // Add one month in test
                : creationDate;
        }

        public byte[] CreateFileBytes(List<SwedbankPayment> payments)
        {
            var lines = CreateRows(payments);
            return Encoding.GetEncoding("ISO-8859-1").GetBytes(string.Join(Environment.NewLine, lines));
        }
        bool IsNordeaPersonKonto(BankAccountNumberSe ban) => ban.BankName.EqualsIgnoreCase("nordea") && new[] { "3300", "3782" }.Contains(ban.ClearingNr);

        public List<string> CreateRows(List<SwedbankPayment> payments)
        {
            var lines = new List<string>();

            lines.Add(CreateOpeningRow());

            var totalPaidAmount = 0m;
            var identifierIncrement = 1;
            foreach (var payment in payments)
            {
                lines.Add(CreatePaymentRow(payment, identifierIncrement, out var identifier));
                if (payment.ToBankAccount.AccountType == BankAccountNumberTypeCode.BankGiroSe ||
                    payment.ToBankAccount.AccountType == BankAccountNumberTypeCode.PlusGiroSe)
                {
                    lines.Add(payment.Ocr != null
                        ? CreatePaymentOcrRow(payment, identifier)
                        : CreateUnstructuredMessageNonOcrRow(payment, identifier));
                }

                totalPaidAmount += payment.Amount;
                identifierIncrement++;
            }

            lines.Add(CreateEndRow(totalPaidAmount));

            return lines;
        }

        private string CreateOpeningRow()
        {
            var creator = new RowCreator(180);
            creator.AddValue(2, 1, "posttyp", "05");
            creator.AddValue(6, 3, "avtalsnr", Settings.AgreementNumber);
            creator.AddValue(50, 9, "filler", creator.Blank(50));
            creator.AddValue(8, 59, "utbetalningsdatum", FileCreationDate.ToString("yyyyMMdd"));
            creator.AddValue(5, 67, "avtalsnr återrapportering", Settings.AgreementNumber.Substring(1, 5)); // not sure, call support
            creator.AddValue(10, 72, "organisationsnummer", Settings.ClientOrgNr.NormalizedValue);
            creator.AddValue(99, 82, "filler", creator.Blank(99));

            return creator.GetRowString();
        }

        private string CreatePaymentRow(SwedbankPayment pmnt, int increment, out string identifier)
        {
            var date = FileCreationDate.ToString("yyyy-MM-dd HH:mm");

            // Increment to ensure unique rows even if same loan has multiple outgoing payments. 
            identifier = ClipRight($"{date} {increment} {pmnt.PaymentId}", 44);

            // Swedbank defines first position as 01, so start from that number and not the indexnumber. 
            // Should a future client use Swedbank as "registerhållaare", we need to send in kundnr and kundnamn
            var creator = new RowCreator(180);
            creator.AddValue(2, 1, "posttyp", "30");
            creator.AddValue(6, 3, "avtalsnr", Settings.AgreementNumber);
            creator.AddValue(44, 9, "id-utbetalning", creator.FillBlankOnRight(44, identifier));
            creator.AddValue(6, 53, "distributionskod-1", creator.Zeroes(6));
            creator.AddValue(3, 59, "utbetalningssätt", creator.Blank(3));
            creator.AddValue(12, 62, "kundnr", creator.Blank(12));
            creator.AddValue(36, 74, "kundnamn", creator.Blank(36));
            creator.AddValue(10, 110, "inforad", GetInfoRad(pmnt.UnstructuredMessage, pmnt.ToBankAccount));
            creator.AddValue(1, 120, "kod-frånvaro", creator.Blank(1));
            creator.AddValue(2, 121, "typ-utbetalning", Settings.AgreementPaymentType);
            creator.AddValue(5, 123, "clearingnr", creator.FillZeroFromLeft(5, ClearingNrOrNull(pmnt.ToBankAccount)));
            creator.AddValue(10, 128, "kontonr", creator.FillZeroFromLeft(10, GetAccountNr(pmnt.ToBankAccount)));
            creator.AddValue(2, 138, "kod-konto", KodKontoOrNull(pmnt.ToBankAccount) ?? creator.Blank(2));
            creator.AddValue(2, 140, "nr-bilaga", creator.Zeroes(2));
            creator.AddValue(15, 142, "belopp-utbetalt", creator.FillZeroFromLeft(15, GetAmount(pmnt.Amount)));
            creator.AddValue(15, 157, "belopp-avdrag", creator.Zeroes(15));
            creator.AddValue(6, 172, "distributionskod-2", creator.Blank(6));
            creator.AddValue(3, 178, "id-bilaga", creator.Zeroes(3));

            return creator.GetRowString();
        }
        private string ClipRight(string source, int maxLength) => source != null && source.Length > maxLength ? source.Substring(0, maxLength) : source;

        /// <summary>
        /// 15000.50m -> "1500050"
        /// I.e. gives in "ören" in the file. 
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static string GetAmount(decimal amount)
        {
            return amount.ToString("0.00", CultureInfo.InvariantCulture).Replace(".", "");
        }

        /// <summary>
        /// Only send in for regular bankaccounts, not for BG/PG. 
        /// </summary>
        /// <returns></returns>
        private string GetInfoRad(string unstructuredMessage, IBankAccountNumber account)
        {
            if (account.AccountType == BankAccountNumberTypeCode.BankAccountSe)
                return ClipRight(unstructuredMessage ?? Settings.CustomerPaymentTransactionMessage, 10).PadRight(10, ' ');

            return string.Empty.PadRight(10, ' ');
        }

        public string ClearingNrOrNull(IBankAccountNumber account)
        {
            if (account.AccountType == BankAccountNumberTypeCode.BankAccountSe)
            {
                var acc = (BankAccountNumberSe)account;
                if (!IsNordeaPersonKonto(acc))
                    return acc.ClearingNr; // Do not set clearingnr for Nordea personkonto. 
            }

            return null;
        }

        private string GetAccountNr(IBankAccountNumber account)
        {
            switch (account.AccountType)
            {
                case BankAccountNumberTypeCode.BankAccountSe:
                    return ((BankAccountNumberSe)account).AccountNr;
                case BankAccountNumberTypeCode.BankGiroSe:
                    return ((BankGiroNumberSe)account).NormalizedValue;
                case BankAccountNumberTypeCode.PlusGiroSe:
                    return ((PlusGiroNumberSe)account).NormalizedValue;
                default:
                    throw new Exception("Bank account type not supported for SUS-file. ");
            }
        }

        public string KodKontoOrNull(IBankAccountNumber account)
        {
            switch (account.AccountType)
            {
                case BankAccountNumberTypeCode.BankAccountSe:
                    var acc = (BankAccountNumberSe)account;
                    if (IsNordeaPersonKonto(acc))
                        return "PK";
                    else
                        return null;
                case BankAccountNumberTypeCode.BankGiroSe:
                    return "BG";
                case BankAccountNumberTypeCode.PlusGiroSe:
                    return "PG";
                default:
                    throw new Exception("Bank account type not supported for SUS-file. ");
            }
        }

        /// <summary>
        /// "Posttyp 65 används till textrader per utbetalning (utbetalningsunikt) för insättningsbesked, SPU, girering
        /// till vanligt Plusgirokonto(ej OCR) och BG."
        /// </summary>
        private string CreateUnstructuredMessageNonOcrRow(SwedbankPayment pmnt, string identifier)
        {
            var creator = new RowCreator(180);
            creator.AddValue(2, 1, "posttyp", "65");
            creator.AddValue(6, 3, "avtalsnr", Settings.AgreementNumber);
            creator.AddValue(44, 9, "id-utbetalning", creator.FillWhitespaceFromRight(44, identifier));
            creator.AddValue(6, 53, "filler", creator.Blank(6));
            creator.AddValue(3, 59, "radnr", "001"); // It is possible to add multiple posttype 65 rows connected to the same payment. 
            creator.AddValue(64, 62, "text", creator.FillWhitespaceFromRight(64, GetLimitedUnstructuredMessage(pmnt)));
            creator.AddValue(55, 126, "text 2", creator.Blank(55)); // Not used for BG/PG

            return creator.GetRowString();
        }

        private string CreatePaymentOcrRow(SwedbankPayment pmnt, string identifier)
        {
            var creator = new RowCreator(180);
            creator.AddValue(2, 1, "posttyp", "66");
            creator.AddValue(6, 3, "avtalsnr", Settings.AgreementNumber);
            creator.AddValue(44, 9, "id-utbetalning", creator.FillWhitespaceFromRight(44, identifier));
            creator.AddValue(6, 53, "filler", creator.Blank(6));
            creator.AddValue(3, 59, "radnr", "001"); // Value fixed according to documentation
            creator.AddValue(25, 62, "referens", creator.FillWhitespaceFromRight(25, pmnt.Ocr));
            creator.AddValue(94, 87, "filler", creator.Blank(94));

            return creator.GetRowString();
        }

        private string CreateEndRow(decimal totalPaidAmount)
        {
            var creator = new RowCreator(180);
            creator.AddValue(2, 1, "posttyp", "80");
            creator.AddValue(6, 3, "avtalsnr", Settings.AgreementNumber);
            creator.AddValue(50, 9, "filler", creator.Blank(50));
            creator.AddValue(15, 59, "summa-utbetalt", creator.FillZeroFromLeft(15, GetAmount(totalPaidAmount)));
            creator.AddValue(15, 74, "summa-korr-plus", creator.Zeroes(15)); // ?
            creator.AddValue(15, 89, "summa-korr-minus", creator.Zeroes(15)); // ?
            creator.AddValue(15, 104, "summa-avdrag", creator.Zeroes(15));
            creator.AddValue(15, 119, "summa-avdrag-korr-plus", creator.Zeroes(15));
            creator.AddValue(15, 134, "summa-avdrag-korr-minus", creator.Zeroes(15));
            creator.AddValue(32, 149, "filler", creator.Blank(32));

            return creator.GetRowString();
        }

        /// <summary>
        /// Limit length according to documentation. Can be multiple rows (not used by us) but max length per row. 
        /// </summary>
        /// <param name="pmnt"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string GetLimitedUnstructuredMessage(SwedbankPayment pmnt)
        {
            switch (pmnt.ToBankAccount.AccountType)
            {
                case BankAccountNumberTypeCode.BankGiroSe:
                    return ClipRight(pmnt.UnstructuredMessage, 50);
                case BankAccountNumberTypeCode.PlusGiroSe:
                    return ClipRight(pmnt.UnstructuredMessage, 35);
                default:
                    throw new Exception("Only used for BG or PG. ");
            }
        }

        /// <summary>
        /// Must be valid OCR to create a post of type 66. 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool IsValidOcrNumber(string value)
        {
            return OcrNumberSe.TryParse(value, out _, out _);
        }

        public class SwedbankPayment
        {
            public IBankAccountNumber ToBankAccount { get; set; }
            /// <summary>
            /// For easier identification of a payment afterwards. Set as part of id-utbetalning.  
            /// </summary>
            public string PaymentId { get; set; }
            public decimal Amount { get; set; }
            public string UnstructuredMessage { get; set; }
            public string Ocr { get; set; }
        }

        public class SwedbankSeSettings
        {
            public OrganisationNumberSe ClientOrgNr { get; set; }
            public string FileFormat { get; set; } // Ex SUS
            public string CustomerPaymentTransactionMessage { get; set; } // What is shown on the bank statement
            public string AgreementNumber { get; set; } // Number of the agreement
            public string AgreementPaymentType { get; set; } // Specified agreement payment type, ex 05
        }
    }

    /// <summary>
    /// The idea for this class is to force documentation in the code of what the file is populating.
    /// Please have this in mind when doing changes to it. 
    /// </summary>
    public class RowCreator
    {
        private int? MaxRowLength { get; set; }
        // For easier debugging. 
        private List<Field> Fields { get; set; }

        public RowCreator(int? maxRowLength = null)
        {
            Fields = new List<Field>();
            MaxRowLength = maxRowLength;
        }

        /// <summary>
        /// Adds whitespace in a specific size. Called "blank" in the documentation. 
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public string Blank(int size)
        {
            return new string(' ', size);
        }

        /// <summary>
        /// Adds zeroes in a specific size. When field in documentation requires this format but we don't use the field.
        /// Another way of "blanking". Ex. field avdrag which is only used for lön-payments. 
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public string Zeroes(int size)
        {
            return new string('0', size);
        }

        public void AddValue(int size, int start, string name, string value)
        {
            if (value == null)
                throw new Exception("Value should not be null, something must have gone wrong. Field: " + name);

            if (value.Length != size)
                throw new Exception("Value should have been prepopulated to fit the field size. Field: " + name);

            if (Fields.Any() && Fields.Last()?.EndPosition != start - 1)
                throw new Exception($"Wrong calculation on start positions for SUS-file, field: {name}, should be: " +
                                    $"{start} but was {Fields.Last()?.EndPosition + 1}");

            var field = new Field
            {
                Size = size,
                StartPosition = start,
                Name = name,
                Value = value
            };

            if (MaxRowLength != null && field.EndPosition > MaxRowLength)
                throw new Exception(($"Field {name} cannot be {size} because the row cannot be longer than {MaxRowLength} but was {field.EndPosition}"));

            Fields.Add(field);
        }

        public string FillBlankOnRight(int size, string value)
        {
            if (value?.Length > size)
                throw new Exception($"Value is {value.Length} but should be maximum {size}, something must have gone wrong. ");

            return value + new string(' ', size - (value?.Length ?? 0));
        }

        /// <summary>
        /// Ex. clearingnr can be 4 characters (6133) but needs to be 5, so it adds 0 in the beginning.
        /// Field that are defined as "N" in their documentation. "Högerjusterat och nollfyllt." 
        /// </summary>
        public string FillZeroFromLeft(int size, string value)
        {
            if (value != null && value.Length == size)
                return value;

            return new string('0', size - (value?.Length ?? 0)) + value;
        }

        /// <summary>
        /// Fills the field with whitespaces on the right side if the value does not fill the size of the field.
        /// Called "AN" in documentation: "Vänsterställt och blankutfyllt fält"
        /// </summary>
        /// <param name="size"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public string FillWhitespaceFromRight(int size, string value)
        {
            if (!(value.Length < size))
                return value;

            return value + new string(' ', size - value.Length);
        }

        public string GetRowString()
        {
            var length = Fields.Sum(field => field.Size);

            if (MaxRowLength != null && length != MaxRowLength)
                throw new Exception($"Length of the row must be exactly {MaxRowLength} but was {length}, something went wrong somewhere. ");

            return string.Join("", Fields.Select(x => x.Value));
        }

        public class Field
        {
            public int Size { get; set; }
            public int StartPosition { get; set; }
            public int EndPosition => (StartPosition + Size) - 1;
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }
}