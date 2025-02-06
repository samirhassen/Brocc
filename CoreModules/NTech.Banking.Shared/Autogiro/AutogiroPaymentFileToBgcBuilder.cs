using NTech.Banking.BankAccounts.Se;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.Autogiro
{
    public class AutogiroPaymentFileToBgcBuilder : AbstractAutogiroFileToBgcBuilder
    {
        private readonly string clientBankGiroCustomerNr;
        private readonly BankGiroNumberSe clientBankGiroNr;
        private readonly Func<DateTime> now;
        private readonly bool isTest;
        private readonly bool isTargetBankGiroLink;
               
        private AutogiroPaymentFileToBgcBuilder(string clientBankGiroCustomerNr, BankGiroNumberSe clientBankGiroNr, Func<DateTime> now, bool isTest, bool isTargetBankGiroLink)
        {
            this.clientBankGiroCustomerNr = clientBankGiroCustomerNr;
            this.clientBankGiroNr = clientBankGiroNr;
            this.now = now;
            this.isTest = isTest;
            this.isTargetBankGiroLink = isTargetBankGiroLink;
        }

        public static AutogiroPaymentFileToBgcBuilder New(string clientBankGiroCustomerNr, BankGiroNumberSe clientBankGiroNr, Func<DateTime> now, bool isTest, bool isTargetBankGiroLink = false)
        {
            var b = new AutogiroPaymentFileToBgcBuilder(clientBankGiroCustomerNr, clientBankGiroNr, now, isTest, isTargetBankGiroLink);
            b.NewRow("01")
                .DateOnly(now()) //Skrivdag
                .String("AUTOGIRO", 8) //Layoutnamn
                .Space(44) //Reserv
                .String(clientBankGiroCustomerNr, 6, rightAligned: true, paddingChar: '0') //Betalningsmottagarens Kundnummer
                .String(clientBankGiroNr.NormalizedValue, 10, rightAligned: true, paddingChar: '0') //Betalningsmottagarens bankgironummer
                .Space(2)
                .End();
            return b;
        }

        /// <summary>
        /// Also known as ag-dragning
        /// </summary>
        public void AddPaymentFromCustomerToClient(decimal amount, DateTime? paymentDate, string payerNr, string ocrNr)
        {
            var r = NewRow("82");

            //Betalningsdag 
            if (paymentDate.HasValue)
                r = r.DateOnly(paymentDate.Value);
            else
                r = r.String("GENAST", 8);

            r
                .String("0", 1) //Periodkod. 0 means once
                .Space(3) //Antal Självförnyande uppdrag. Not in use with period once
                .Space(1) //Reserv
                .String(payerNr, 16, rightAligned: true, paddingChar: '0')
                .Money(amount, 12)
                .String(clientBankGiroNr.NormalizedValue, 10, rightAligned: true, paddingChar: '0') //Betalningsmottagarens bankgironummer
                .String(ocrNr, 16) //Referens
                .Space(11)
                .End();
        }

        /// <summary>
        /// Like a repayment
        /// </summary>
        public void AddPaymentFromClientToCustomer(DateTime paymentDate, decimal amount, string payerNr, string referenceText)
        {
            //Parameter order is intentionally different to make it less likely a developer will pick the wrong one by accident
            NewRow("32")
                .DateOnly(paymentDate)
                .String("0", 1) //Periodkod. 0 means once
                .Space(3) //Antal Självförnyande uppdrag. Not in use with period once
                .Space(1) //Reserv
                .String(payerNr, 16, rightAligned: true, paddingChar: '0')
                .Money(amount, 12)
                .String(clientBankGiroNr.NormalizedValue, 10, rightAligned: true, paddingChar: '0') //Betalningsmottagarens bankgironummer
                .String(referenceText, 16) //Referens
                .Space(11)
                .End();
        }

        public override string GetFileName()
        {
            var d = now();
            var dataSetName = isTest ? "IAGZZ" : "IAGAG";
            var nr = clientBankGiroCustomerNr.PadLeft(6, '0');
            return $"BFEP.{dataSetName}.K0{nr}.D{d.ToString("yyMMdd")}.T{d.ToString("HHmmss")}";
        }
    }
}
