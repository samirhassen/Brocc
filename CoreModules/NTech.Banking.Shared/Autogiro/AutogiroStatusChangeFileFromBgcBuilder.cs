using NTech.Banking.BankAccounts.Se;
using NTech.Banking.CivicRegNumbers.Se;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.Autogiro
{
    public class AutogiroStatusChangeFileFromBgcBuilder : AbstractAutogiroFileToBgcBuilder
    {
        private readonly string clientBankGiroCustomerNr;
        private readonly BankGiroNumberSe clientBankGiroNr;
        private readonly Lazy<DateTime> writeDate;
        private readonly bool isTest;
        private readonly bool isTargetBankGiroLink;
        private int statusChangeItemCount = 0;
        private bool hasEndBeenWritten = false;               

        private AutogiroStatusChangeFileFromBgcBuilder(string clientBankGiroCustomerNr, BankGiroNumberSe clientBankGiroNr, Lazy<DateTime> writeDate, bool isTest, bool isTargetBankGiroLink)
        {
            this.clientBankGiroCustomerNr = clientBankGiroCustomerNr;
            this.clientBankGiroNr = clientBankGiroNr;
            this.writeDate = writeDate;
            this.isTest = isTest;
            this.isTargetBankGiroLink = isTargetBankGiroLink;
        }

        public static AutogiroStatusChangeFileFromBgcBuilder New(string clientBankGiroCustomerNr, BankGiroNumberSe clientBankGiroNr, Lazy<DateTime> writeDate, bool isTest, bool isTargetBankGiroLink = false)
        {
            var b = new AutogiroStatusChangeFileFromBgcBuilder(clientBankGiroCustomerNr, clientBankGiroNr, writeDate, isTest, isTargetBankGiroLink);
            b.NewRow("01")
                .String("AUTOGIRO", 20) //Layoutnamn
                .Space(2)
                .DateOnly(writeDate.Value) //Skrivdag
                .Space(12)
                .String("AG-MEDAVI", 20)
                .String(clientBankGiroCustomerNr, 6, rightAligned: true, paddingChar: '0') //Betalningsmottagarens Kundnummer
                .String(clientBankGiroNr.NormalizedValue, 10, rightAligned: true, paddingChar: '0') //Betalningsmottagarens bankgironummer
                .End();
            return b;
        }

        public void AddStatusChangeItem(string payerNr, BankAccountNumberSe bankAccountNumber, CivicRegNumberSe civicRegNr, string infoKod, string kommentarskod)
        {
            if (hasEndBeenWritten)
                throw new Exception("End post written, cannot add more items");

            NewRow("73")
                .String(clientBankGiroNr.NormalizedValue, 10, rightAligned: true, paddingChar: '0') //Betalningsmottagarens bankgironummer
                .String(payerNr?.TrimStart('0'), 16, rightAligned: true, paddingChar: '0') //Betalarnummer
                .String(bankAccountNumber?.ClearingNr, 4, rightAligned: true, paddingChar: '0') //TODO: Swedbank with 5th digit
                .String(bankAccountNumber?.AccountNr, 12, rightAligned: true, paddingChar: '0')
                .String(civicRegNr?.NormalizedValue, 12)
                .Space(5)
                .String(infoKod, 2)
                .String(kommentarskod, 2)
                .Space(8) //Åtgärdsdatum
                .Space(7)
                .End();
            statusChangeItemCount++;
        }
        
        public void AddAcceptedActivation(string payerNr, BankAccountNumberSe bankAccountNumber, CivicRegNumberSe civicRegNr)
        {
            AddStatusChangeItem(payerNr, bankAccountNumber, civicRegNr, "04", "32");
        }

        public void AddAcceptedClientInitiatedCancellation(string payerNr)
        {
            AddStatusChangeItem(payerNr, null, null, "03", "33");
        }

        public void AddAcceptedCustomerInitiatedCancellation(string payerNr, BankAccountNumberSe bankAccountNumber, CivicRegNumberSe civicRegNr)
        {
            AddStatusChangeItem(payerNr, bankAccountNumber, civicRegNr, "46", "02");
        }

        protected override void BeforeWrite()
        {
            if (!hasEndBeenWritten)
            {
                NewRow("09")
                    .DateOnly(writeDate.Value) //Skrivdag
                    .String("9999", 4)
                    .String(statusChangeItemCount.ToString(), 7, rightAligned: true, paddingChar: '0')
                    .Space(59)
                    .End();
                hasEndBeenWritten = true;
            }
            base.BeforeWrite();
        }

        public override string GetFileName()
        {
            var d = writeDate.Value;
            var dataSetName = isTest ? "UAGZZ" : "UAGU4";
            var nr = clientBankGiroCustomerNr.PadLeft(6, '0');
            return $"BFEP.{dataSetName}.K0{nr}.D{d.ToString("yyMMdd")}.T{d.ToString("HHmmss")}";
        }
    }
}
