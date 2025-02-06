using nCredit.Code;
using nCredit.Code.Fileformats;
using NTech.Banking.BankAccounts;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.BankAccounts.Se;
using NTech.Banking.Conversion;
using NTech.Banking.OutgoingPaymentFiles;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class NewOutgoingPaymentFileBusinessEventManager : BusinessEventManagerOrServiceBase
    {
        private readonly BankAccountNumberParser bankAccountNumberParser;
        private readonly IDocumentClient documentClient;
        private readonly EncryptionService encryptionService;
        private readonly ICreditEnvSettings envSettings;

        public NewOutgoingPaymentFileBusinessEventManager(INTechCurrentUserMetadata currentUser, IDocumentClient documentClient, 
            IClientConfigurationCore clientConfiguration, ICoreClock clock, EncryptionService encryptionService,
            ICreditEnvSettings envSettings) : base(currentUser, clock, clientConfiguration)
        {
            this.bankAccountNumberParser = new BankAccountNumberParser(clientConfiguration.Country.BaseCountry);
            this.documentClient = documentClient;
            this.encryptionService = encryptionService;
            this.envSettings = envSettings;
        }

        public List<OutgoingPaymentFileHeader> Create(ICreditContextExtended context, bool skipWhenNoPaymentsExist = false)
        {
            var result = new List<OutgoingPaymentFileHeader>();

            var pending = context
                .OutgoingPaymentHeadersQueryable
                .Select(x => new
                {
                    Header = x,
                    x.Items,
                    ShouldBePaidToCustomerBalance = x.Transactions.Where(y => y.AccountCode == TransactionAccountType.ShouldBePaidToCustomer.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m,
                    CreditNr = x.Transactions.Where(y => y.CreditNr != null).Select(y => y.CreditNr).FirstOrDefault()
                })
                .Where(x => x.ShouldBePaidToCustomerBalance > 0m && !x.Header.OutgoingPaymentFileHeaderId.HasValue)
                .ToList();

            if (skipWhenNoPaymentsExist && pending.Count == 0)
                return result;

            var evt = new BusinessEvent
            {
                BookKeepingDate = Clock.Today,
                ChangedById = UserId,
                ChangedDate = Clock.Now,
                EventType = BusinessEventType.NewOutgoingPaymentFile.ToString(),
                InformationMetaData = InformationMetadata,
                TransactionDate = Clock.Today,
                EventDate = Clock.Now
            };
            context.AddBusinessEvent(evt);

            Func<List<Payment>, OutgoingPaymentFileHeader> addPaymentFile = (payments) =>
                {
                    var outgoingFile = new OutgoingPaymentFileHeader
                    {
                        BookKeepingDate = evt.BookKeepingDate,
                        ChangedById = evt.ChangedById,
                        ChangedDate = evt.ChangedDate,
                        InformationMetaData = evt.InformationMetaData,
                        CreatedByEvent = evt,
                        TransactionDate = evt.TransactionDate
                    };
                    context.AddOutgoingPaymentFileHeaders(outgoingFile);
                    foreach (var p in payments)
                    {
                        p.PaymentHeader.OutgoingPaymentFile = outgoingFile;
                        context.AddAccountTransactions(
                            CreateTransaction(TransactionAccountType.ShouldBePaidToCustomer, -p.Amount, evt.BookKeepingDate, evt, outgoingPaymentId: p.PaymentHeader.Id, creditNr: p.CreditNr));
                    }
                    result.Add(outgoingFile);
                    return outgoingFile;
                };

            var encryptedItems = pending.SelectMany(x => x.Items).Where(x => x.IsEncrypted).ToArray();
            IDictionary<long, string> decryptedValues;
            if (encryptedItems.Any())
            {
                decryptedValues = encryptionService.DecryptEncryptedValues(context, encryptedItems.Select(x => long.Parse(x.Value)).ToArray());
            }
            else
            {
                decryptedValues = new Dictionary<long, string>();
            }

            Func<OutgoingPaymentHeader, Dictionary<OutgoingPaymentHeaderItemCode, string>> itemsToDict = x =>
                {
                    var d = new Dictionary<OutgoingPaymentHeaderItemCode, string>();
                    foreach (var i in x.Items)
                    {
                        var name = Enums.Parse<OutgoingPaymentHeaderItemCode>(i.Name);
                        if (name.HasValue)
                        {
                            d[name.Value] = i.IsEncrypted ? decryptedValues[long.Parse(i.Value)] : i.Value;
                        }
                    }
                    return d;
                };

            var allPayments = pending.Select(x =>
            {
                var d = itemsToDict(x.Header);
                return new Payment
                {
                    Amount = x.ShouldBePaidToCustomerBalance,
                    CustomerName = d.Req(OutgoingPaymentHeaderItemCode.CustomerName),
                    Message = d.Req(OutgoingPaymentHeaderItemCode.CustomerMessage),
                    ToBankAccount = ParseToBankAccount(itemsToDict(x.Header)),
                    PaymentHeader = x.Header,
                    FromBankAccountNr = d.Opt(OutgoingPaymentHeaderItemCode.FromBankAccountNr),
                    FromIban = d.Opt(OutgoingPaymentHeaderItemCode.FromIban),
                    CreditNr = x.CreditNr,
                    PaymentReference = d.Opt(OutgoingPaymentHeaderItemCode.PaymentReference)
                };
            }
            ).ToList();
                        
            if (envSettings.OutgoingPaymentFilesBankName == "danskebankfi")
            {
                Add_DanskeBankFi(allPayments, addPaymentFile);
            }
            else if (envSettings.OutgoingPaymentFilesBankName == "handelsbankense")
            {
                var bbanPayments = allPayments.Where(x => x.ToBankAccount.AccountType == BankAccountNumberTypeCode.BankAccountSe).ToList();
                var nonBbanPayments = allPayments.Where(x => x.ToBankAccount.AccountType != BankAccountNumberTypeCode.BankAccountSe).ToList();

                Add_HandelsbankenSe(bbanPayments, addPaymentFile);
                if (nonBbanPayments.Any())
                    Add_ExcelSe(nonBbanPayments, addPaymentFile);
            }
            else if (envSettings.OutgoingPaymentFilesBankName == "swedbankse")
            {
                Add_SwedbankSe(allPayments, addPaymentFile);
            }
            else if (envSettings.OutgoingPaymentFilesBankName == "excelse")
            {
                Add_ExcelSe(allPayments, addPaymentFile);
            }
            else
            {
                throw new Exception("Bank not supported: " + envSettings.OutgoingPaymentFilesBankName);
            }

            return result;
        }

        private void Add_SwedbankSe(List<Payment> payments, Func<List<Payment>, OutgoingPaymentFileHeader> addPaymentFile)
        {
            if (ClientCfg.Country.BaseCountry != "SE")
                throw new Exception("Fileformat swedbankse only valid for country SE");

            var sbSettings = envSettings.OutgoingPaymentFilesSwedbankSeSettings;

            var fileFormat = new OutgoingPaymentFileFormat_SUS_SE(envSettings.IsProduction, sbSettings, Clock.Now.DateTime);
            var fileBytes = fileFormat.CreateFileBytes(payments.Select(p => new OutgoingPaymentFileFormat_SUS_SE.SwedbankPayment
            {
                PaymentId = p.CreditNr,
                ToBankAccount = p.ToBankAccount,
                Amount = p.Amount,
                Ocr = p.PaymentReference,
                UnstructuredMessage = p.Message
            }).ToList());

            var archiveKey = documentClient.ArchiveStore(fileBytes, "text/plain",
                $"Outgoingpayments-SUS-{Clock.Now.ToString("yyyy-MM-dd")}.txt");

            var outgoingFile = addPaymentFile(payments);
            outgoingFile.FileArchiveKey = archiveKey;
        }

        private void Add_ExcelSe(List<Payment> payments, Func<List<Payment>, OutgoingPaymentFileHeader> addPaymentFile)
        {
            if (ClientCfg.Country.BaseCountry != "SE")
                throw new Exception("Fileformat excelse only valid for country SE");

            var groups = new[]
                {
                        new OutgoingPaymentFileFormat_ExcelSe.PaymentFile.PaymentGroup
                        {
                            Payments  = payments
                                .Select(x => new  OutgoingPaymentFileFormat_ExcelSe.Payment
                                {
                                    Amount = x.Amount,
                                    CustomerName = x.CustomerName,
                                    Message = x.Message,
                                    Reference = x.PaymentReference,
                                    ToBankAccount = x.ToBankAccount
                                }).ToList()
                        }
                    }.ToList();
            var creator = new OutgoingPaymentFileFormat_ExcelSe();
            var fileName = $"OutgoingPayments-{Clock.Now.ToString("yyyy-MM-dd")}-{Clock.Now.ToString("HHmmss")}.xlsx";
            var archiveKey = creator.CreateExcelFileInArchive(new OutgoingPaymentFileFormat_ExcelSe.PaymentFile { ExecutionDate = Clock.Today, Groups = groups }, Clock.Now, fileName, documentClient);

            var outgoingFile = addPaymentFile(payments);
            outgoingFile.FileArchiveKey = archiveKey;
        }

        private void Add_DanskeBankFi(List<Payment> payments, Func<List<Payment>, OutgoingPaymentFileHeader> addPaymentFile)
        {
            if (ClientCfg.Country.BaseCountry != "FI")
                throw new Exception("Fileformat danskebankfi only valid for country FI");

            var settings = envSettings.OutgoingPaymentFilesDanskeBankSettings;
            if (settings.FileFormat != "pain.001.001.03")
                throw new Exception("Fileformat not supported: " + settings.FileFormat);

            //NOTE: No reason to think this cant be done. It just hasn't been done yet.
            if (payments.Any(x => !string.IsNullOrWhiteSpace(x.PaymentReference)))
                throw new Exception("PaymentReference not implemented");

            //Create the file
            var groups =
                payments
                    .GroupBy(x => x.FromIban)
                    .Select((x, i) => new OutgoingPaymentFileFormat_Pain_001_001_3.PaymentFile.PaymentGroup
                    {
                        FromIban = IBANFi.Parse(x.Key),
                        Payments = x.Select((y, j) => new OutgoingPaymentFileFormat_Pain_001_001_3.Payment
                        {
                            Amount = y.Amount,
                            CustomerName = y.CustomerName,
                            Message = y.Message,
                            ToIban = (IBANFi)y.ToBankAccount,
                        }).ToList()
                    })
                    .ToList();

            var f = new OutgoingPaymentFileFormat_Pain_001_001_3.PaymentFile
            {
                CurrencyCode = "EUR",
                //One month in the future in test by recommendation from the banks integration department.
                //It gives the users time to discover if a test file got into production by accident. The test flag in the header is also meant to deal with this problem
                ExecutionDate = envSettings.IsProduction ? Clock.Today : Clock.Today.AddMonths(1),
                SenderCompanyId = settings.SendingCompanyId,
                SenderCompanyName = settings.SendingCompanyName,
                SendingBankBic = settings.SendingBankBic,
                SendingBankName = settings.SendingBankName,
                Groups = groups
            };

            var creator = new OutgoingPaymentFileFormat_Pain_001_001_3(envSettings.IsProduction);
            creator.PopulateIds(f);

            var externalId = f.PaymentFileId;
            var fileBytes = creator.CreateFileAsBytes(f, Clock.Now);
            var fileName = $"OutgoingPayments-{Clock.Now.ToString("yyyy-MM-dd")}-{externalId}.xml";
            var archiveKey = documentClient.ArchiveStore(fileBytes, "application/xml", fileName);

            var outgoingFile = addPaymentFile(payments);
            outgoingFile.ExternalId = externalId;
            outgoingFile.FileArchiveKey = archiveKey;
        }

        private void Add_HandelsbankenSe(List<Payment> payments, Func<List<Payment>, OutgoingPaymentFileHeader> addPaymentFile)
        {
            if (ClientCfg.Country.BaseCountry != "SE")
                throw new Exception("Fileformat danskebankfi only valid for country SE");

            var settings = envSettings.OutgoingPaymentFilesHandelsbankenSeSettings;
            if (settings.FileFormat != "pain.001.001.03")
                throw new Exception("Fileformat not supported: " + settings.FileFormat);

            //NOTE: No reason to think this cant be done. It just hasn't been done yet.
            if (payments.Any(x => !string.IsNullOrWhiteSpace(x.PaymentReference)))
                throw new Exception("PaymentReference not implemented");

            var groups =
                payments
                    .GroupBy(x => x.FromBankAccountNr)
                    .Select((x, i) => new OutgoingPaymentFileFormat_Pain_001_001_3_SE.PaymentFile.PaymentGroup
                    {
                        FromAccount = BankAccountNumberSe.Parse(x.Key),
                        Payments = x.Select((y, j) => new OutgoingPaymentFileFormat_Pain_001_001_3_SE.Payment
                        {
                            Amount = y.Amount,
                            CustomerName = y.CustomerName,
                            Message = y.Message,
                            ToAccount = y.ToBankAccount
                        }).ToList()
                    })
                    .ToList();

            var creator = new OutgoingPaymentFileFormat_Pain_001_001_3_SE(envSettings.IsProduction);
            var f = new OutgoingPaymentFileFormat_Pain_001_001_3_SE.PaymentFile
            {
                CurrencyCode = "SEK",
                ExecutionDate = envSettings.IsProduction ? Clock.Today : Clock.Today.AddMonths(1),
                SenderCompanyName = settings.SenderCompanyName,
                Groups = groups
            };
            creator.PopulateIds(f);

            var externalId = f.PaymentFileId;

            var fileBytes = creator.CreateFileAsBytes(f, Clock.Now.DateTime, settings.ClientOrgnr, settings.BankMmbId);
            var fileName = $"OutgoingPayments-{Clock.Now.ToString("yyyy-MM-dd")}-{externalId}.xml";
            var archiveKey = documentClient.ArchiveStore(fileBytes, "application/xml", fileName);

            var outgoingFile = addPaymentFile(payments);
            outgoingFile.ExternalId = externalId;
            outgoingFile.FileArchiveKey = archiveKey;
        }

        private class Payment
        {
            public decimal Amount { get; internal set; }
            public string CustomerName { get; internal set; }
            public string Message { get; internal set; }
            public string FromBankAccountNr { get; set; }
            public string FromIban { get; set; }
            public IBankAccountNumber ToBankAccount { get; internal set; }
            public OutgoingPaymentHeader PaymentHeader { get; set; }
            public string CreditNr { get; set; }
            public string PaymentReference { get; set; }
        }

        private IBankAccountNumber ParseToBankAccount(Dictionary<OutgoingPaymentHeaderItemCode, string> items)
        {
            return bankAccountNumberParser.ParseFromStringWithDefaults(
                items.Opt(OutgoingPaymentHeaderItemCode.ToIban) ?? items.Opt(OutgoingPaymentHeaderItemCode.ToBankAccountNr),
                items.Opt(OutgoingPaymentHeaderItemCode.ToBankAccountNrType));
        }
    }
}