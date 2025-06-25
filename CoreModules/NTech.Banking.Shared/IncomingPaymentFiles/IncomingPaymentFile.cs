using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.BankAccounts.Se;
using NTech.Banking.OrganisationNumbers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using NTech.Banking.Shared.BankAccounts.Fi;

namespace NTech.Banking.IncomingPaymentFiles
{
    public class IncomingPaymentFile
    {
        public string Format { get; set; }
        public string ExternalId { get; set; }
        public DateTime ExternalCreationDate { get; set; }
        public List<Account> Accounts { get; set; }        
        public List<Warning> Warnings { get; set; }

        public class Account
        {
            public BankAccountNr AccountNr { get; set; }
            public string Currency { get; set; }
            public List<AccountDateBatch> DateBatches { get; set; }
            public List<NonPaymentTransaction> NonPaymentTransactions { get; set; }
        }

        public class AccountDateBatch
        {
            public DateTime BookKeepingDate { get; set; }
            public List<AccountDateBatchPayment> Payments { get; set; }            
        }

        public class NonPaymentTransaction
        {
            public string ExternalId { get; set; }
            public decimal Amount { get; set; }
            public int Count { get;set; }
            public string Description { get;set; }
        }

        public class AccountDateBatchPayment
        {
            public string ExternalId { get; set; }
            public decimal Amount { get; set; }
            public string OcrReference { get; set; }
            public string CustomerName { get; set; }
            public string CustomerAddressCountry { get; set; }
            public string CustomerAddressStreetName { get; set; }
            public string CustomerAddressBuildingNumber { get; set; }
            public string CustomerAddressPostalCode { get; set; }
            public string CustomerAddressTownName { get; set; }
            public List<string> CustomerAddressLines { get; set; }
            public string InformationText { get; set; }
            public BankAccountNr CustomerAccountNr { get; set; }
            public IOrganisationNumber CustomerOrgnr { get; set; }
            public string AutogiroPayerNumber { get; set; }            
        }

        public class BankAccountNr
        {
            private readonly BankGiroNumberSe bankGiroNumber;
            private readonly IBANFi iban;

            public BankAccountNr(IBANFi iban)
            {
                this.iban = iban;
            }

            public BankAccountNr(BankGiroNumberSe bankGiroNumber)
            {
                this.bankGiroNumber = bankGiroNumber;
            }

            public string NormalizedValue
            {
                get
                {
                    if (iban != null)
                        return iban.NormalizedValue;
                    
                    if (bankGiroNumber != null)
                        return bankGiroNumber.NormalizedValue;

                    throw new NotImplementedException();
                }
            }
        }

        public class Warning
        {
            public string Message { get; set; }
            public string OcrReference { get; set; }
            public string AutogiroPayerNumber { get; set; }
        }
    }

    public class IncomingPaymentFileWithOriginal : IncomingPaymentFile
    {
        public byte[] OriginalFileData { get; set; }
        public string OriginalFileName { get; set; }
    }
}
