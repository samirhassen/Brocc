using NTech.Banking.Autogiro;
using NTech.Banking.IncomingPaymentFiles;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace nCredit.Code.Fileformats
{
    public class IncomingPaymentFileFormat_Autogiro : IIncomingPaymentFileFormat
    {
        private readonly bool isProduction;

        public IncomingPaymentFileFormat_Autogiro(bool isProduction)
        {
            this.isProduction = isProduction;
        }

        public string FileFormatName
        {
            get
            {
                return "autogiro";
            }
        }

        public bool MightBeAValidFile(byte[] filedata)
        {
            if (filedata == null || filedata.Length == 0)
                return false;
            return AutoGiroIncomingPaymentsFileParser.CouldBeThisFiletype(new MemoryStream(filedata));
        }

        public bool TryParse<TIncomingPaymentFile>(byte[] filedata, out TIncomingPaymentFile paymentFile, out string errorMessage) where TIncomingPaymentFile : IncomingPaymentFile, new()
        {
            using (var stream = new MemoryStream(filedata))
            {
                var f = new AutoGiroIncomingPaymentsFileParser().Parse(stream);
                try
                {

                    var pf = new TIncomingPaymentFile();
                    pf.ExternalCreationDate = f.BankGiroFileWriteDate;
                    pf.ExternalId = f.ExternalFileId;
                    var account = new IncomingPaymentFile.Account
                    {
                        AccountNr = new IncomingPaymentFile.BankAccountNr(f.PaymentReceiverBankGiroNumber),
                        Currency = "SEK"
                    };
                    pf.Accounts = new List<IncomingPaymentFile.Account> { account };
                    account.DateBatches = f
                        .Payments.GroupBy(x => x.PaymentDate)
                        .Select(x => new IncomingPaymentFile.AccountDateBatch
                        {
                            BookKeepingDate = x.Key,
                            Payments = x.Select(y => new IncomingPaymentFile.AccountDateBatchPayment
                            {
                                Amount = y.PaymentAmount,
                                OcrReference = y.ReferenceNumber,
                                AutogiroPayerNumber = y.PayerNumber
                            }).ToList()
                        })
                        .ToList();

                    if (f.Warnings != null && f.Warnings.Any())
                    {
                        pf.Warnings = f.Warnings.Select(x => new IncomingPaymentFile.Warning
                        {
                            AutogiroPayerNumber = x.PayerNumber,
                            OcrReference = x.ReferenceNumber,
                            Message = x.Message
                        }).ToList();
                    };

                    errorMessage = null;
                    paymentFile = pf;
                    return true;
                }
                catch (AutogiroParserException ex)
                {
                    errorMessage = ex.Message;
                    paymentFile = null;
                    return false;
                }
            }
        }

        public bool IsSupportedInCountry(string countryIsoCode)
        {
            return countryIsoCode == "SE";
        }
    }
}