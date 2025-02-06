using NTech.Banking.BankAccounts.Se;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.Autogiro
{
    public class AutoGiroIncomingPaymentsFileParser
    {
        private static AutogiroFileParser CreateParser()
        {
            return AutogiroFileParser
                        .NewParser(false)
                        .IgnoreTransactionCodes("16", "32", "17", "77")

                        .NewHeaderRow("01")
                        .NewField("Skrivdag", 25, 20)
                        .NewField("Innehåll", 45, 20)
                        .NewField("Betalningsmottagarens Kundnummer", 65, 6)
                        .NewField("Betalningsmottagarens bankgironummer", 71, 10)
                        .EndRow()

                        .NewRepeatingRow("15", nestedTransactionCodes: new List<string> { "82" })
                        .NewField("Betalningsmottagarens bankontonummer", 3, 35)
                        .NewField("Betalningsdag", 38, 8)
                        .NewField("Insättningsbelopp", 51, 18)
                        .NewField("Antal betalningsposter", 72, 8)
                        .EndRow()

                        .NewRepeatingRow("82")
                        .NewField("Betalningsdag", 3, 8)
                        .NewField("Betalarnummer", 16, 16)
                        .NewField("Belopp", 32, 12)
                        .NewField("Betalningsreferens", 54, 16)
                        .NewField("Betalningsstatuskod", 80, 1)
                        .EndRow()
                        
                        .NewFooterRow("09")
                        .NewField("Antal insättningsposter", 15, 6) //tk15
                        .NewField("Total antal inbetalningsposter TK82", 21, 12) //tk82
                        .NewField("Antalet uttagsposter", 33, 6) //tk16
                        .NewField("Antalet utbetalningsposter TK32", 39, 12) //tk32
                        .NewField("Antalet uttagsposter avseende återbetalningar", 51, 6) //tk17
                        .NewField("Total antal återbetalnings-poster TK77", 57, 12) //tk77
                        .EndRow();
        }

        public class Result
        {
            public string PaymentReceiverBankGiroCustomerNumber { get; set; }
            public DateTime BankGiroFileWriteDate { get; set; }
            public string ExternalFileId { get; set; }
            public BankGiroNumberSe PaymentReceiverBankGiroNumber { get; set; }
            public AutogiroFileParser.ParsedFile RawFile { get; set; }
            public List<IncomingPayment> Payments { get; set; }
            public List<Warning> Warnings { get; set; }
        }

        public class IncomingPayment
        {
            public string PaymentReceiverBankAccountNumber { get; set; }
            public DateTime PaymentDate { get; set; }
            public decimal PaymentAmount { get; set; }
            public string PayerNumber { get; set; }
            public string ReferenceNumber { get; set; }
        }

        public class Warning
        {
            public string Message { get; set; }
            public string PayerNumber { get; set; }
            public string ReferenceNumber { get; set; }            
        }

        public Result Parse(Stream s)
        {
            var f = CreateParser().Parse(s);

            var actualContentType = f.Header.GetString("Innehåll");
            if (actualContentType != ExpectedContentType)
                throw new AutogiroParserException($"Invalid file. Expected innehåll to be '{ExpectedContentType}' but was instead '{actualContentType}'");

            var r = new Result
            {
                BankGiroFileWriteDate = f.Header.GetDate("Skrivdag", isPrefix: true), //Only the first 8 yeay, month, day
                ExternalFileId = f.Header.GetString("Skrivdag"), //All 20 chars
                PaymentReceiverBankGiroCustomerNumber = f.Header.GetString("Betalningsmottagarens Kundnummer"),
                PaymentReceiverBankGiroNumber = f.Header.GetBankgiroNr("Betalningsmottagarens bankgironummer"),
                Payments = new List<IncomingPayment>(),
                Warnings = new List<Warning>(),
                RawFile = f
            };

            foreach (var i in f.Items.Where(x => x.Row.TransactionCode == "15"))
            {
                var paymentReceiverBankAccountNumber = i.GetNumberString("Betalningsmottagarens bankontonummer");
                var paymentDate = i.GetDate("Betalningsdag");
                var totalDepositAmount = i.GetDecimal("Insättningsbelopp");
                var nrOfDeposits = i.GetInt("Antal betalningsposter");

                foreach (var p in i.NestedLines.Where(x => x.Row.TransactionCode == "82"))
                {                    
                    var innerPaymentDate = p.GetDate("Betalningsdag");
                    var payerNumber = p.GetNumberString("Betalarnummer");
                    var paymentAmount = p.GetDecimal("Belopp");
                    var paymentReference = p.GetNumberString("Betalningsreferens");
                    var paymentStatusCode = p.GetString("Betalningsstatuskod");

                    if(paymentStatusCode == "0")
                    {
                        r.Payments.Add(new IncomingPayment
                        {
                            PayerNumber = payerNumber,
                            PaymentAmount = paymentAmount,
                            ReferenceNumber = paymentReference,
                            PaymentReceiverBankAccountNumber = paymentReceiverBankAccountNumber,
                            PaymentDate = innerPaymentDate
                        });
                    }
                    else
                    {
                        r.Warnings.Add(new Warning
                        {
                            Message = $"Payment with reference '{paymentReference}' and payer number '{payerNumber}' was rejected with status {paymentStatusCode}",
                            PayerNumber = payerNumber,
                            ReferenceNumber = paymentReference
                        });
                    }
                }
            }
             
            var countTk15 = f.Footer.GetInt("Antal insättningsposter");
            var countTk82 = f.Footer.GetInt("Total antal inbetalningsposter TK82");
            var countTk16 = f.Footer.GetInt("Antalet uttagsposter");
            var countTk32 = f.Footer.GetInt("Antalet utbetalningsposter TK32");
            var countTk17 = f.Footer.GetInt("Antalet uttagsposter avseende återbetalningar");
            var countTk77 = f.Footer.GetInt("Total antal återbetalnings-poster TK77");

            var actualTk15Count = f.Items.Where(x => x.Row.TransactionCode == "15").Count();
            if (countTk15 != actualTk15Count)
                r.Warnings.Add(new Warning { Message = $"The footer claims {countTk15} payment groups (tk15) but the file contains {actualTk15Count}" });
            var actualTk82Count = r.Payments.Count; //Seems to only count payments with status code 0
            if(countTk82 != actualTk82Count)
                r.Warnings.Add(new Warning { Message = $"The footer claims {countTk82} payments (tk82) but the file contains {actualTk82Count}" });

            if (countTk16 > 0)
                r.Warnings.Add(new Warning { Message = $"The file contains {countTk16} uttagsposter (tk16). These will be ignored." });

            if (countTk32 > 0)
                r.Warnings.Add(new Warning { Message = $"The file contains {countTk32} utbetalningsposter (tk32). These will be ignored." });

            if (countTk17 > 0)
                r.Warnings.Add(new Warning { Message = $"The file contains {countTk17} uttagsposter avseende återbetalningar (tk17). These will be ignored." });

            if (countTk77 > 0)
                r.Warnings.Add(new Warning { Message = $"The file contains {countTk77} återbetalnings-poster (tk77). These will be ignored." });

            return r;
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
                var actualContentType = pResult.Header.GetString("Innehåll");
                return actualContentType == ExpectedContentType;
            }
            catch (AutogiroParserException)
            {
                return false;
            }
        }

        private const string ExpectedContentType = "BET. SPEC & STOPP TK";
    }
}
