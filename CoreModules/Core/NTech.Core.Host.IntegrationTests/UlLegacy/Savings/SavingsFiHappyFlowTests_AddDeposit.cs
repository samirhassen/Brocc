using Moq;
using nCustomer;
using Newtonsoft.Json;
using NTech.Banking.CivicRegNumbers.Fi;
using NTech.Core.Customer.Database;
using NTech.Core.Customer.Shared.Services;
using NTech.Core.Savings.Shared.Database;
using NTech.Core.Savings.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NTech.Core.Savings.Database;
using NTech.Core.Module.Shared.Clients;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.Shared.BankAccounts.Fi;
using NTech.Core.Savings.Shared.BusinessEvents;

namespace NTech.Core.Host.IntegrationTests.UlLegacy.Savings
{
    public partial class SavingsFiHappyFlowTests
    {
        private void AddDeposit(UlLegacyTestRunner.TestSupport support)
        {
            /*
             Add one payment of 1000 from manual unplaced and one payment of 100 from a file
             */
            string savingsAccountNr = "S20030";

            var unplacedMgr = new NewManualIncomingPaymentBatchBusinessEventManager(support.CurrentUser, support.Clock, support.ClientConfiguration);
            var customerClient = TestPersons.CreateRealisticCustomerClient(support);
            var documentClient = new Mock<IDocumentClient>(MockBehavior.Strict);
            documentClient
                .Setup(x => x.ArchiveStore(It.IsAny<byte[]>(), "application/xml", "test.txt"))
                .Returns("78efa1d5-12e8-4f28-bc5b-a0ce129106d4.txt");
            var fileMgr = new NewIncomingPaymentFileBusinessEventManager(support.CurrentUser, support.Clock, support.ClientConfiguration,
                support.SavingsEnvSettings, support.EncryptionService, support.CreateSavingsContextFactory(), customerClient.Object);

            using (var context = support.CreateSavingsContextFactory().CreateContext())
            {
                var evt = unplacedMgr.CreateBatch(context, new NewManualIncomingPaymentBatchBusinessEventManager.ManualPayment
                {
                    Amount = 1000m,
                    BookkeepingDate = support.Clock.Now.Date,
                    InitiatedByUserId = support.CurrentUser.UserId,
                    NoteText = "B1"
                });

                context.SaveChanges();
                var paymentId = context.IncomingPaymentHeadersQueryable
                    .Where(x => x.Transactions.Any(y => y.BusinessEventId == evt.Id)).Select(x => x.Id).Single();
                Assert.IsTrue(fileMgr.TryPlaceFromUnplaced(paymentId, savingsAccountNr, 1000m, 0m, out var failedPlayMessage), failedPlayMessage);
            }            

            using (var context = support.CreateSavingsContextFactory().CreateContext())
            {
                context.BeginTransaction();
                try
                {

                    var ocrDepositReference = context.SavingsAccountHeadersQueryable.Where(x => x.SavingsAccountNr == savingsAccountNr)
                        .SelectMany(x => x.DatedStrings).Where(x => x.Name == "OcrDepositReference").Select(x => x.Value).Single();

                    var placeResult = fileMgr.ImportIncomingPaymentFile(context, new Banking.IncomingPaymentFiles.IncomingPaymentFileWithOriginal
                    {
                        ExternalId = "abc123",
                        ExternalCreationDate = support.Clock.Today,
                        Format = "whatever",
                        OriginalFileData = Encoding.UTF8.GetBytes("test"),
                        OriginalFileName = "test.txt",
                        Accounts = new List<Banking.IncomingPaymentFiles.IncomingPaymentFile.Account>
                    {
                        new Banking.IncomingPaymentFiles.IncomingPaymentFile.Account
                        {
                            AccountNr = new Banking.IncomingPaymentFiles.IncomingPaymentFile.BankAccountNr(IBANFi.Parse("FI9340534400707976")),
                            Currency = "EUR",
                            DateBatches = new List<Banking.IncomingPaymentFiles.IncomingPaymentFile.AccountDateBatch>
                            {
                                new Banking.IncomingPaymentFiles.IncomingPaymentFile.AccountDateBatch
                                {
                                    BookKeepingDate = support.Clock.Today,
                                    Payments = new List<Banking.IncomingPaymentFiles.IncomingPaymentFile.AccountDateBatchPayment>
                                    {
                                        new Banking.IncomingPaymentFiles.IncomingPaymentFile.AccountDateBatchPayment
                                        {
                                            Amount = 100m,
                                            OcrReference = ocrDepositReference,
                                            CustomerName = "Test person"
                                        }
                                    }
                                }
                            }
                        }
                    }
                    }, documentClient.Object, out var placedMessage);

                    context.SaveChanges();
                    context.CommitTransaction();
                }
                catch
                {
                    context.RollbackTransaction();
                    throw;
                }
            }
        }
    }
}
