using NTech.Banking.BankAccounts.Se;
using NTech.Banking.CivicRegNumbers.Se;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.Autogiro
{
    public class AutogiroMedgivandeAviseringFileParser
    {
        private static AutogiroFileParser CreateParser()
        {
            return AutogiroFileParser
                        .NewParser(false)

                        .NewHeaderRow("01")
                        .NewField("Skrivdag", 25, 8)
                        .NewField("Innehåll", 45, 20)
                        .NewField("Betalningsmottagarens Kundnummer", 65, 6)
                        .NewField("Betalningsmottagarens bankgironummer", 71, 10)
                        .EndRow()

                        .NewRepeatingRow("73")
                        .NewField("Betalningsmottagarens bankgironummer", 3, 10)
                        .NewField("Betalarnummer", 13, 16)
                        .NewField("Bankkontonummer", 29, 16)
                        .NewField("Personnummer", 45, 12)
                        .NewField("Informationskod", 62, 2)
                        .NewField("Kommentarskod", 64, 2)
                        .NewField("Åtgärdsdatum", 66, 8)
                        .EndRow()

                        .NewFooterRow("09")
                        .NewField("Antal poster", 15, 7)
                        .EndRow();
        }

        public static bool CouldBeThisFiletype(Stream s)
        {
            try
            {
                var pResult = AutogiroFileParser
                            .NewParser(true)
                            .NewHeaderRow("01")
                            .NewField("Innehåll", 45, 20)
                            .EndRow()
                            .Parse(s);
                return pResult.Header.GetString("Innehåll") == ExpectedContentType;
            }
            catch (AutogiroParserException)
            {
                return false;
            }
        }

        private const string ExpectedContentType = "AG-MEDAVI";

        public File Parse(Stream file)
        {
            var p = CreateParser();
            var f = p.Parse(file);
            var r = new File();
            var innehall = f.Header.GetString("Innehåll");
            if (innehall != ExpectedContentType)
                throw new AutogiroParserException($"Innehåll was expected to be '{ExpectedContentType}' but was instead '{innehall}'. Did you try to import the wrong type of file?");

            r.BankGiroNr = f.Header.GetBankgiroNr("Betalningsmottagarens bankgironummer");
            r.WrittenDate = f.Header.GetDate("Skrivdag");
            r.ClientBgcCustomerNr = f.Header.GetString("Betalningsmottagarens Kundnummer");
            r.ResultItems = new List<AgMedgivandeResultItem>();

            foreach (var a in f.Items)
            {
                var betalarNr = a.GetNumberString("Betalarnummer");
                var bankAccountNr = a.GetNumberString("Bankkontonummer");
                var bankgiroNr = a.GetBankgiroNr("Betalningsmottagarens bankgironummer");
                var personnr = a.GetString("Personnummer");
                var infoKod = a.GetString("Informationskod");
                var kommentarskod = a.GetString("Kommentarskod");

                var i = new AgMedgivandeResultItem
                {
                    CommentCode = kommentarskod,
                    InformationCode = infoKod,
                    PaymentNr = betalarNr,
                    BankGiroNr = bankgiroNr
                };
                if ((infoKod == "03" && kommentarskod == "33") || infoKod == "43" || infoKod=="44" || infoKod == "46")
                {
                    //Makulering
                    i.Action = AgActionCode.Cancel;
                }
                else if(infoKod == "04" && kommentarskod == "32")
                {
                    i.Action = AgActionCode.Start;
                }

                if(personnr.Length > 0 && !(personnr.StartsWith("00") || personnr.StartsWith("99"))) //00,99 -> orgnr
                {
                    CivicRegNumberSe c;
                    if (!CivicRegNumberSe.TryParse(personnr, out c))
                        throw new AutogiroParserException("Personnummer: Invalid");
                    i.CustomerCivicRegNr = c;
                }
                if(bankAccountNr.Length > 0)
                {
                    BankAccountNumberSe b;
                    string em;
                    if(!BankAccountNumberSe.TryParse(bankAccountNr, out b, out em))
                        throw new AutogiroParserException($"Bankkontonummer: {em}");
                    i.CustomerBankAccountNr = b;
                }

                r.ResultItems.Add(i);
            }

            return r;
        }

        public class File
        {
            public BankGiroNumberSe BankGiroNr { get; set; }
            public string ClientBgcCustomerNr { get; set; }
            public DateTime WrittenDate { get; set; }
            public List<AgMedgivandeResultItem> ResultItems { get; set; }
        }

        public class AgMedgivandeResultItem
        {
            public string InformationCode { get; set; }
            public string CommentCode { get; set; }
            public AgActionCode? Action { get; set; }
            public CivicRegNumberSe CustomerCivicRegNr { get; set; }
            public BankAccountNumberSe CustomerBankAccountNr { get; set; }

            public BankGiroNumberSe BankGiroNr { get; set; }
            public string PaymentNr { get; set; }
        }

        public enum AgActionCode
        {
            Cancel,
            Start
        }
    }
}
