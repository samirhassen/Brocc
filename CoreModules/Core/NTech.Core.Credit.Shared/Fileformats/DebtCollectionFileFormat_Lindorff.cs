using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Clients;
using System;
using System.IO;
using System.Linq;

namespace nCredit.Code.Fileformats
{
    public class DebtCollectionFileFormat_Lindorff
    {
        public class LindorfSettings
        {
            public int ClientNumber { get; set; }
        }

        private static string TranslateLanguage(string language)
        {
            var v = (language ?? "").ToLowerInvariant();
            if (v == "fi" || v == "en")
                return v.ToUpper();
            else if (v == "sv")
                return "RU"; //... really lindorff?
            else
                return "FI";
        }

        public string CreateFileInArchive(DebtCollectionFileModel f, LindorfSettings settings, DateTimeOffset now, string filename, IDocumentClient documentClient)
        {
            using (var ms = new MemoryStream())
            using (var w = LindorffFileWriter.BeginCreate(ms))
            {
                WriteLindorffFile(f, w, settings);
                return documentClient.ArchiveStore(ms.ToArray(), "text/plain", filename);
            }
        }

        private void WriteLindorffFile(DebtCollectionFileModel file, LindorffFileWriter w, LindorfSettings settings)
        {
            foreach (var c in file.Credits)
            {
                w
                    .BeginRecord(10, "Assignment")
                    .WriteUnsignedInt(settings.ClientNumber, 6, "Client number")
                    .WriteString(c.CreditNr, 20, "Reference number") //Will be shown to end user. Other alternative would be ocr
                    .WriteString("", 8, "Assignment class")
                    .WriteString("V", 1, "Collection form") //V = Normal collection
                    .WriteString("", 15, "Exceptional bankaccount")
                    .WriteString(c.Currency, 3, "Currency")
                    .WriteString("", 1, "Publishing code")
                    .WriteString("", 1, "Starting code") //blank = start now. K means start after confirmation only. Always put K in testfiles maybe?
                    .WriteString("", 22, "Reserved") //Cobol...
                    .WriteString("E", 1, "Business claim") //K = business E = not business. Guessing it referes to the debtors.
                    .WriteString("", 8, "Reserved 2")
                    .WriteString("", 1, "Summons permission")
                    .EndRecord();

                w
                    .BeginRecord(11, "Supplementary info 1 - Credit") //Including even though optional
                    .WriteString("K", 1, "Consumer credit indicator") //Are we this? Guessing so
                    .WriteDate(c.StartDate, 8, "Credit signing date")
                    .WriteDate(c.TerminationLetterDueDate, 8, "Credit maturity date")
                    .WriteString("", 61, "Reserved")
                    .EndRecord();

                //Supplementary info 2 - IBAN/BIC. Skipping this since optional and this info will be really old at this point
                //Supplementary info 9 - Creditors name. Seems pointless since always the same

                //TODO: Order by applicant nr then customerid

                foreach (var dd in c.OrderedCustomersWithRoles)
                {
                    var d = dd.Item1;
                    w
                        .BeginRecord(20, "Debtor record 1 - type and name")
                        .WriteString("", 2, "Type of obligation")
                        .WriteString(d.IsCompany ? d.CompanyName : $"{d.LastName} {d.FirstName}", 75, "Name")
                        .WriteString(d.IsCompany ? "E" : "", 1, "Juridical form") //blank = person, E = company
                        .EndRecord();

                    w
                        .BeginRecord(21, "Debtor record 2 - address")
                        .WriteString(d.Adr?.Street, 30, "Address")
                        .WriteString(d.Adr?.Zipcode, 5, "Postal code")
                        .WriteString(d.Adr?.City, 24, "Postoffice")
                        .WriteString(d.Adr?.Country, 3, "Address country")
                        .WriteString(TranslateLanguage(d.PreferredLanguage), 2, "Language")
                        .WriteString(d.CivicRegNrOrOrgnr, 11, "Identity nr")
                        .WriteString("L", 1, "Address type") //Have no idea what this is. L is the only option and it is accounting addres
                        .WriteString(d.CivicRegNrOrOrgnrCountry, 2, "Debtor reg country")
                        .EndRecord();

                    w
                        .BeginRecord(22, "Debtor record 3 - Phone nr")
                        .WriteString(d.Phone, 16, "Phone nr")
                        .WriteString("", 20, "Reserve")
                        .WriteString("", 30, "Contact person")
                        .WriteString("", 2, "Letter identifier")
                        .WriteString("", 1, "Payment disorder publishing") //Really? Interesting name...
                        .WriteString("", 1, "Suspension of ...")
                        .WriteString("", 8, "Reserve 2")
                        .EndRecord();

                    w
                        .BeginRecord(24, "Debtor record 5 - Email")
                        .WriteString(d.Email, 60, "Email")
                        .WriteString("", 18, "Reserve")
                        .EndRecord();

                    //Skipped 6 which again is phonenr
                }

                var capitalUniqueId = PaymentOrderItem.FromAmountType(DomainModel.CreditDomainModel.AmountType.Capital).GetUniqueId();
                var totalCapitalDebt = c.NotNotifiedCapitalAmount + c.Notifications.Aggregate(0m, (x, y) => x + (y.Amounts.ContainsKey(capitalUniqueId) ? y.Amounts[capitalUniqueId] : 0m));
                w
                    .BeginRecord(30, "Claim record 1")
                    .WriteString("", 2, "Type of claim")
                    .WriteUnsignedDecimal(totalCapitalDebt, 10, 2, "Capital of claim")
                    .WriteDate(c.TerminationLetterDueDate, 6, "Original due date")
                    .WriteUnsignedDecimal(c.InterestRatePercent, 10, 2, "Credit interest")
                    .WriteUnsignedDecimal(0m, 5, 3, "Overdue interest rate")
                    .WriteUnsignedDecimal(0m, 10, 2, "Overdue interest amount")
                    .WriteString("", 6, "")
                    .EndRecord();

                //Skipped claim record 2 since it seems to be the same type of things as claim record 1 but with random fields
                //like credit interest type with no reasonable guidance on what they are

                //Skipped claim record 3 which is something to do with public law

                //Comments. Skipped these
            }

            //Stora frågor: 
            //- Hur synka vad de räknar ränta på och från när? Dröjsmål vs vanlig?
            //- Var är allt som inte är kapital? Hittar bara kapital och ränta och jag förstår mig inte på räntan alls
        }
    }
}