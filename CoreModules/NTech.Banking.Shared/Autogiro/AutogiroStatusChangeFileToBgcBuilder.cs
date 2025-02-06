using NTech.Banking.BankAccounts.Se;
using NTech.Banking.CivicRegNumbers.Se;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.Autogiro
{
    public class AutogiroStatusChangeFileToBgcBuilder : AbstractAutogiroFileToBgcBuilder
    {
        private readonly string clientBankGiroCustomerNr;
        private readonly BankGiroNumberSe clientBankGiroNr;
        private readonly Func<DateTime> now;
        private readonly bool isTest;
        private readonly bool isTargetBankGiroLink;
               
        private AutogiroStatusChangeFileToBgcBuilder(string clientBankGiroCustomerNr, BankGiroNumberSe clientBankGiroNr, Func<DateTime> now, bool isTest, bool isTargetBankGiroLink)
        {
            this.clientBankGiroCustomerNr = clientBankGiroCustomerNr;
            this.clientBankGiroNr = clientBankGiroNr;
            this.now = now;
            this.isTest = isTest;
            this.isTargetBankGiroLink = isTargetBankGiroLink;
        }

        public static AutogiroStatusChangeFileToBgcBuilder New(string clientBankGiroCustomerNr, BankGiroNumberSe clientBankGiroNr, Func<DateTime> now, bool isTest, bool isTargetBankGiroLink = false)
        {
            var b = new AutogiroStatusChangeFileToBgcBuilder(clientBankGiroCustomerNr, clientBankGiroNr, now, isTest, isTargetBankGiroLink);
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

        public void AddCancellation(string paymentNr)
        {
            NewRow("03")
                .String(clientBankGiroNr.NormalizedValue, 10, rightAligned: true, paddingChar: '0') //Betalningsmottagarens bankgironummer
                .String(paymentNr?.TrimStart('0'), 16, rightAligned: true, paddingChar: '0') //Betalarnummer
                .Space(52)
                .End();
        }

        public void AddActivation(string paymentNr, CivicRegNumberSe bankAccountOwnerCivicRegNr, BankAccountNumberSe bankAccountNr)
        {
            NewRow("04")
                .String(clientBankGiroNr.NormalizedValue, 10, rightAligned: true, paddingChar: '0') //Betalningsmottagarens bankgironummer
                .String(paymentNr?.TrimStart('0'), 16, rightAligned: true, paddingChar: '0') //Betalarnummer
                .String(bankAccountNr.ClearingNr, 4, rightAligned: true, paddingChar: '0') //TODO: Swedbank with 5th digit
                .String(bankAccountNr.AccountNr, 12, rightAligned: true, paddingChar: '0')
                .String(bankAccountOwnerCivicRegNr.NormalizedValue, 12)
                .Space(20)
                .Space(2) //Internetbank-fält
                .Space(2)
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
