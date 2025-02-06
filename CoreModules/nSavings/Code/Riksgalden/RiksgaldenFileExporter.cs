using NTech.Banking.CivicRegNumbers;
using System;
using System.Collections.Generic;
using System.IO;

namespace nSavings.Code.Riksgalden
{
    public class RiksgaldenFileExporter
    {
        private readonly DateTime writeDate;
        private readonly string instituteName;
        private readonly string instituteOrgnr;

        public RiksgaldenFileExporter(DateTime writeDate, string instituteName, string instituteOrgnr)
        {
            this.writeDate = writeDate;
            this.instituteName = instituteName;
            this.instituteOrgnr = instituteOrgnr;
        }

        public class FiFilialCustomer
        {
            /// <summary>
            /// Se Customer
            /// </summary>
            public string Kundnummer { get; set; }

            /// <summary>
            /// Förnamn för en insättare som är en fysisk person, eventuellt flera namn separerade med mellanslag. Ska vara tomt för en insättare som är en juridisk person.
            /// Max 150 tecken. Får vara tomt.
            /// </summary>
            public string Fornamn { get; set; }

            /// <summary>
            /// Efternamn för en insättare som är en fysisk person och fullständig firma för en insättare som är en juridisk person.
            /// (framgår inga regel i filspecen men rimligen max 150 tecken och får bara vara tomt om uppgiften saknas.)
            /// </summary>
            public string Efternamn { get; set; }

            /// <summary>
            /// Titel, "sr", "jr" eller liknande om sådan information finns tillgänglig.
            /// </summary>
            public string Titel { get; set; }
        }

        public class FiFilialAccount
        {
            /// <summary>
            /// Se account
            /// </summary>
            public string Kontonummer { get; set; }

            /// <summary>
            /// ISO-koden för det land som kontot är registrerat i.
            /// </summary>
            public string Landskod { get; set; }
        }

        /// <summary>
        /// Kundfilen ska innehålla en förteckning över samtliga ersättningsberättigade insättare i institutet. Institutet ska rensa för insättare som inte omfattas av insättningsgarantin.
        /// </summary>
        public class Customer
        {
            /// <summary>
            /// Kundnummer Ett unikt nummer för insättaren. Maximalt 50 tecken. Får inte vara tomt 
            /// </summary>
            public string Kundnummer { get; set; }

            /// <summary>
            /// Samtliga namn för en insättare som är en fysisk person och fullständig firma för en insättare som är en juridisk person. Maximalt 150 tecken. Får inte vara tomt. Maximalt 50 tecken.
            /// </summary>
            public string Namn { get; set; }

            /// <summary>
            /// För en insättare som är en svensk juridisk person ska fältet innehålla organisationsnummer.
            /// 
            /// För en utländsk insättare hos en filial ska fältet innehålla utländskt person-, organisations- eller identitetsnummer om det är känt och giltigt. Framför numret ska anges prefix med landskod enligt ISO 3166-1. Svenska personeller organisationsnummer ska inte anges med prefix "SE".
            /// </summary>
            public ICivicRegNumber PersonOrgNummer { get; set; }

            /// <summary>
            /// Insättarens utdelningsadress. Får vara tomt om institutet saknar informationen. Maximalt 250 tecken.
            /// </summary>
            public string Utdelningsadress { get; set; }

            /// <summary>
            /// Insättarens postnummer. Får endast vara tomt om institutet saknar informationen. Maximalt 250 tecken.
            /// </summary>
            public string Postnummer { get; set; }

            /// <summary>
            /// Insättarens postort. Får endast vara tomt om institutet saknar informationen. Maximalt 250 tecken.
            /// </summary>
            public string Ort { get; set; }

            /// <summary>
            /// Det land där insättaren har sin registrerade adress. Anges bara om annat land än Sverige. Får endast vara tomt om institutet saknar informationen. Maximalt 250 tecken.
            /// </summary>
            public string Postland { get; set; }

            /// <summary>
            /// ISO-koden för det land som insättaren är registrerad i. Landskod enligt ISO 3166-1. Får inte vara tomt. 2 tecken
            /// </summary>
            public string Landskod { get; set; }

            /// <summary>
            /// C/O-namn. Får vara tomt. Maximalt 250 tecken.
            /// </summary>
            public string CONamn { get; set; }

            /// <summary>
            /// Tax Identification Number. Skatteregistreringsnummer för insättare som är skattepliktiga i utlandet. Max 100 tecken. Får vara tomt.
            /// </summary>
            public string TIN { get; set; }
        }

        public class Account
        {
            /// <summary>
            /// Kontots nummer. Kontonumret ska vara unikt. Maximalt 50 tecken. Får inte vara tomt.
            /// </summary>
            public string Kontonummer { get; set; }

            /// <summary>
            /// Valutakod enligt ISO 4217. 3 tecken. Får inte vara tomt.
            /// </summary>
            public string Valuta { get; set; }

            /// <summary>
            /// Det saldo som finns bokfört på kontot. Anges i kontots valuta. Ska inte innehålla upplupen ränta. Max 26 tecken. Får inte vara tomt.
            /// </summary>
            public decimal Kapital { get; set; }

            /// <summary>
            /// Den upplupna räntan fram till den dag då ersättningsrätt inträder. Max 26 tecken. Får inte vara tomt.
            /// </summary>
            public decimal UpplupenRanta { get; set; }

            /// <summary>
            /// "Ja" om kontot är pantsatt, "Nej" om konton inte är pantsatt. Får inte vara tomt.
            /// </summary>
            public bool Pantsatt { get; set; }

            /// <summary>
            /// "Ja" om kontot är spärrat för utbetalning till kontohavaren och "Nej" om konton inte är spärrat.
            /// </summary>
            public bool Sparrat { get; set; }
        }

        public class AccountDistribution
        {
            private AccountDistribution()
            {

            }

            public static AccountDistribution CreateEqualAmongAll(string kontonr, string kundnr, int nrOfOwners)
            {
                return new AccountDistribution
                {
                    Kontonummer = kontonr,
                    Kundnummer = kundnr,
                    KundensAndel = Math.Round(1m / ((decimal)nrOfOwners), 6)
                };
            }

            /// <summary>
            /// Se Account
            /// </summary>
            public string Kontonummer { get; set; }

            /// <summary>
            /// Se Customer
            /// </summary>
            public string Kundnummer { get; set; }

            /// <summary>
            /// Andel i decimalform där 1 är högst och 0 är lägst. minst en och maximalt sex decimaler. För att undvika avrundningsfel vid summeringen av andelarna rekommenderas maximala antalet decimaler. Får inte vara tomt.
            /// </summary>
            public decimal KundensAndel { get; set; }
        }

        public class Transaction
        {
            /// <summary>
            /// Se Account
            /// </summary>
            public string Kontonummer { get; set; }

            /// <summary>
            /// Datum och tidpunkt för transaktionen. Anges i formatet: "ÅÅÅÅ-MM-DD TT:MM:SS". Får inte vara tomt.
            /// </summary>
            public DateTime Transaktionsdatum { get; set; }

            /// <summary>
            /// Datum och tidpunkt för bokföringen. Anges i formatet: "ÅÅÅÅ-MM-DD TT:MM:SS". 
            /// 
            /// Alltid tomt i T1. Aldrig tomt i T2.
            /// 
            /// Obs! När de säger bokföring så menar de 'faktiskt genomförd' i någon slags halvvag mening som
            /// vi valt att tolka som att alla transaktioner är genomförda så fort uppstår utom uttag/avslut
            /// som blir genomförda då utbetalningsfilen skapas.
            /// </summary>
            public DateTime? Bokforingsdatum { get; set; }

            /// <summary>
            /// Vem som är transaktionens betalningsavsändare eller betalningsmottagare. Max 50 tecken. Får vara tomt.
            /// 
            /// Deras exempel är av typen 'Grejer och sånt AB', 'Testbolaget AB' ... svårt att se vad vi kan sätta här som är vettigt.
            /// </summary>
            public string Referens { get; set; }

            /// <summary>
            /// Belopp på transaktionen. Max 26 tekcen. Får inte vara tomt
            /// </summary>
            public decimal Belopp { get; set; }
        }

        public void WriteTransactionsFileToStream(IList<Transaction> transactions, bool isInitialTransactionFile, Stream target)
        {
            RiksgaldenFileBuilder<Transaction>
                .Begin(transactions)
                .AddStringColumn("Kontonummer", true, 50, x => x.Kontonummer)
                .AddDateAndTimeColumn("Transaktionsdatum", true, x => x.Transaktionsdatum)
                .AddDateAndTimeColumn("Bokföringsdatum", !isInitialTransactionFile, x =>
                {
                    if (isInitialTransactionFile && x.Bokforingsdatum.HasValue)
                        throw new Exception("Transactions must not have a bokföringsdatum in the first transaction file");
                    return x.Bokforingsdatum;
                })
                .AddStringColumn("Referens", false, 50, x => x.Referens)
                .AddDecimalColumn("Belopp", true, 26, x => x.Belopp)
                .WriteFileToStream(writeDate, instituteName, instituteOrgnr, target);
        }

        public void WriteCustomerFileToStream(IList<Customer> customers, Stream target)
        {
            RiksgaldenFileBuilder<Customer>
                .Begin(customers)
                .AddStringColumn("Kundnummer", true, 50, x => x.Kundnummer)
                .AddStringColumn("Namn", true, 150, x => x.Namn)
                .AddCivicRegNrColumn("PersonOrgNummer", true, x => x.PersonOrgNummer)
                .AddStringColumn("Utdelningsadress", false, 250, x => x.Utdelningsadress)
                .AddStringColumn("Postnummer", false, 250, x => x.Postnummer)
                .AddStringColumn("Ort", false, 250, x => x.Ort)
                .AddStringColumn("Postland", false, 250, x => x.Postland)
                .AddStringColumn("Landskod", true, 2, x => x.Landskod)
                .AddStringColumn("CONamn", false, 250, x => x.CONamn)
                .AddStringColumn("TIN", false, 100, x => x.TIN)
                .WriteFileToStream(writeDate, instituteName, instituteOrgnr, target);
        }

        public void WriteAccountDistributionsFileToStream(IList<AccountDistribution> accountDistributions, Stream target)
        {
            RiksgaldenFileBuilder<AccountDistribution>
                .Begin(accountDistributions)
                .AddStringColumn("Kontonummer", true, 50, x => x.Kontonummer)
                .AddStringColumn("Kundnummer", true, 50, x => x.Kundnummer)
                .AddDecimalColumn("KundensAndel", true, 8, x => x.KundensAndel, fixedDecimals: 6)
                .WriteFileToStream(writeDate, instituteName, instituteOrgnr, target);
        }


        public void WriteAccountsFileToStream(IList<Account> accounts, Stream target)
        {
            RiksgaldenFileBuilder<Account>
                .Begin(accounts)
                .AddStringColumn("Kontonummer", true, 50, x => x.Kontonummer)
                .AddStringColumn("Valuta", true, 3, x => x.Valuta)
                .AddDecimalColumn("Kapital", true, 26, x => x.Kapital)
                .AddDecimalColumn("UpplupenRänta", true, 26, x => x.UpplupenRanta)
                .AddStringColumn("Pantsatt", true, 3, x => x.Pantsatt ? "Ja" : "Nej")
                .AddStringColumn("Spärrat", true, 3, x => x.Sparrat ? "Ja" : "Nej")
                .WriteFileToStream(writeDate, instituteName, instituteOrgnr, target);
        }

        public void WriteFinnishFilialFileToStream(IList<FiFilialCustomer> customers, IList<FiFilialAccount> accounts, Stream target)
        {
            Func<string, int, string> clipRight = (s, maxLength) => (s ?? "").Length > maxLength ? s.Substring(0, maxLength) : s;

            RiksgaldenFileBuilder<FiFilialAccount>
                .Begin(accounts)
                .AddStringColumn("", true, 2, x => "KL")
                .AddStringColumn("", true, 50, x => x.Kontonummer)
                .AddStringColumn("", true, 2, x => x.Landskod)
                .WriteFileToStream(writeDate, instituteName, instituteOrgnr, target, isFirstFilialBlock: true);

            RiksgaldenFileBuilder<FiFilialCustomer>
                .Begin(customers)
                .AddStringColumn("", true, 2, x => "EF")
                .AddStringColumn("", true, 50, x => x.Kundnummer)
                .AddStringColumn("", false, 150, x => clipRight(x.Efternamn, 150))
                .AddStringColumn("", false, 150, x => clipRight(x.Fornamn, 150))
                .AddStringColumn("", false, 50, x => clipRight(x.Titel, 50))
                .WriteFileToStream(writeDate, instituteName, instituteOrgnr, target, isFirstFilialBlock: false);
        }
    }
}
