using System;
using System.Collections.Generic;
using System.Linq;
using nSavings.Code;
using nSavings.Code.Fileformats;
using NTech.Banking.Shared.BankAccounts.Fi;
using NTech.Core.Savings.Shared.DbModel;

namespace nSavings.DbModel.BusinessEvents
{
    public class NewOutgoingPaymentFileBusinessEventManager : BusinessEventManagerBase
    {
        public NewOutgoingPaymentFileBusinessEventManager(int userId, string informationMetadata) : base(userId,
            informationMetadata)
        {
        }

        public OutgoingPaymentFileHeader Create(SavingsContext context)
        {
            var evt = AddBusinessEvent(BusinessEventType.OutgoingPaymentFileExport, context);

            var outgoingFile = new OutgoingPaymentFileHeader
            {
                CreatedByEvent = evt,
                TransactionDate = evt.TransactionDate,
            };
            FillInInfrastructureFields(outgoingFile);
            context.OutgoingPaymentFileHeaders.Add(outgoingFile);

            var pending = context
                .OutgoingPaymentHeaders
                .Include("Items")
                .Select(x => new
                {
                    Header = x,
                    ShouldBePaidToCustomerBalance =
                        x.Transactions
                            .Where(y => y.AccountCode == LedgerAccountTypeCode.ShouldBePaidToCustomer.ToString())
                            .Sum(y => (decimal?)y.Amount) ?? 0m,
                    SavingsAccountNr = x.Transactions.Where(y => y.SavingsAccountNr != null)
                        .Select(y => y.SavingsAccountNr).FirstOrDefault()
                })
                .Where(x => x.ShouldBePaidToCustomerBalance > 0m && !x.Header.OutgoingPaymentFileHeaderId.HasValue)
                .ToList();

            var encryptedItems = pending.SelectMany(x => x.Header.Items).Where(x => x.IsEncrypted).ToArray();
            IDictionary<long, string> decryptedValues;
            if (encryptedItems.Any())
            {
                decryptedValues = EncryptionContext.Load(context,
                    encryptedItems.Select(x => long.Parse(x.Value)).ToArray(), NEnv.EncryptionKeys.AsDictionary());
            }
            else
            {
                decryptedValues = new Dictionary<long, string>();
            }

            foreach (var p in pending)
            {
                var payment = p.Header;
                payment.OutgoingPaymentFile = outgoingFile;
                AddTransaction(context, LedgerAccountTypeCode.ShouldBePaidToCustomer, -p.ShouldBePaidToCustomerBalance,
                    evt, outgoingFile.TransactionDate,
                    outgoingPaymentId: payment.Id,
                    savingsAccountNr: p.SavingsAccountNr);
            }

            string archiveKey;
            var documentClient = new DocumentClient();
            if (NEnv.OutgoingPaymentFilesBankName == "danskebankfi")
            {
                if (NEnv.ClientCfg.Country.BaseCountry != "FI")
                    throw new Exception("Fileformat danskebankfi only valid for country SE");

                var settings = NEnv.OutgoingPaymentFilesDanskeBankSettings;
                if (settings.FileFormat != "pain.001.001.03")
                    throw new Exception("Fileformat not supported: " + settings.FileFormat);

                //Create the file
                var groups =
                    pending
                        .GroupBy(x => GetItemValue(x.Header, OutgoingPaymentHeaderItemCode.FromIban))
                        .Select((x, i) => new OutgoingPaymentFileFormat_Pain_001_001_3.PaymentFile.PaymentGroup
                        {
                            FromIban = IBANFi.Parse(x.Key),
                            Payments = x.Select((y, j) => new OutgoingPaymentFileFormat_Pain_001_001_3.Payment
                            {
                                Amount = y.ShouldBePaidToCustomerBalance,
                                CustomerName = GetItemValue(y.Header, OutgoingPaymentHeaderItemCode.CustomerName),
                                Message = GetItemValue(y.Header, OutgoingPaymentHeaderItemCode.CustomerMessage),
                                ToIban = IBANFi.Parse(GetItemValue(y.Header, OutgoingPaymentHeaderItemCode.ToIban)),
                            }).ToList()
                        })
                        .ToList();

                var f = new OutgoingPaymentFileFormat_Pain_001_001_3.PaymentFile
                {
                    CurrencyCode = "EUR",
                    //One month in the future in test by recommendation from the banks integration department. 
                    //It gives the users time to discover if a test file got into production by accident. The test flag in the header is also meant to deal with this problem
                    ExecutionDate = NEnv.IsProduction ? Clock.Today : Clock.Today.AddMonths(1),
                    SenderCompanyId = settings.SendingCompanyId,
                    SenderCompanyName = settings.SendingCompanyName,
                    SendingBankBic = settings.SendingBankBic,
                    SendingBankName = settings.SendingBankName,
                    Groups = groups
                };

                var creator = new OutgoingPaymentFileFormat_Pain_001_001_3();
                OutgoingPaymentFileFormat_Pain_001_001_3.PopulateIds(f);

                outgoingFile.ExternalId = f.PaymentFileId;

                var fileBytes = creator.CreateFileAsBytes(f, Clock.Now);
                var fileName = $"Savings-OutPmts-{Clock.Now:yyyy-MM-dd}-{outgoingFile.ExternalId}.xml";
                archiveKey = documentClient.ArchiveStore(fileBytes, "application/xml", fileName);
            }
            else
            {
                throw new Exception("Bank not supported: " + NEnv.OutgoingPaymentFilesBankName);
            }

            outgoingFile.FileArchiveKey = archiveKey;

            return outgoingFile;

            string GetItemValue(OutgoingPaymentHeader h, OutgoingPaymentHeaderItemCode code)
            {
                var i = h.Items.Single(x => x.Name == code.ToString());
                return i.IsEncrypted ? decryptedValues[long.Parse(i.Value)] : i.Value;
            }
        }
    }
}