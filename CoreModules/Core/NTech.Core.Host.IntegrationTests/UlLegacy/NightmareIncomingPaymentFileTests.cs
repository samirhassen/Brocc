using nCredit.DbModel.BusinessEvents;
using nCredit.DomainModel;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.IncomingPaymentFiles;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.UlLegacy.Utilities;
using System.Text;

namespace NTech.Core.Host.IntegrationTests.UlLegacy
{
    public class NightmareIncomingPaymentFileTests
    {
        [Test]
        public void MultiplePaymentOnSameCredit_AreLeftUnplaced()
        {
            var t = new Test(onlyOnePayment: false, assert: context =>
            {
                var hasAnyPlacedTransactions = context
                    .TransactionsQueryable
                    .Any(x => x.IncomingPaymentId != null && (x.CreditNr != null || x.CreditNotificationId != null));
                var countByIsPlaced = context.IncomingPaymentHeadersQueryable.GroupBy(x => x.IsFullyPlaced) //ef core how are you this shit at groupby :/
                    .Select(x => new { IsFullyPlaced = x.Key, Count = x.Count() })
                    .ToList().ToDictionary(x => x.IsFullyPlaced, x => x.Count);
                Assert.That(countByIsPlaced[true], Is.EqualTo(1), "Expected one placed payment");
                Assert.That(countByIsPlaced[false], Is.EqualTo(26), "Expected 26 unplaced payments");
            });
            t.RunTest();
        }

        [Test]
        public void OnePaymentOnSameCredit_IsPlaced()
        {
            var t = new Test(onlyOnePayment: true, assert: context =>
            {
                var hasAnyPlacedTransactions = context
                    .TransactionsQueryable
                    .Any(x => x.IncomingPaymentId != null && (x.CreditNr != null || x.CreditNotificationId != null));
                var isFullyPlaced = context.IncomingPaymentHeadersQueryable.All(x => x.IsFullyPlaced);
                Assert.That(hasAnyPlacedTransactions, Is.True, "Expected placed payments");
                Assert.That(isFullyPlaced, Is.True, "Expected fully placed");
            });
            t.RunTest();
        }

        private class Test : UlLegacyTestRunner
        {
            private readonly bool onlyOnePayment;
            private readonly Action<ICreditContextExtended> assert;

            public Test(bool onlyOnePayment, Action<ICreditContextExtended> assert)
            {
                this.onlyOnePayment = onlyOnePayment;
                this.assert = assert;
            }

            protected override void DoTest()
            {
                const decimal InitialLoanAmount = 5000m;
                var credit = CreditsUlLegacy.CreateCredit(Support, 1, creditAmount: 5000m);
                var creditNr = credit.CreditNr;
                var customerId = credit.CreditCustomers.Single().CustomerId;
                Support.MoveToNextDayOfMonth(14);
                Credits.NotifyCredits(Support, (credit.CreditNr, customerId, NotificationExpectedResultCode.NotificationCreated));

                decimal notificationBalance;
                string ocrPaymentReference;
                using (var context = Support.CreateCreditContextFactory().CreateContext())
                {
                    var notification = CreditNotificationDomainModel.CreateForCredit(creditNr, context, Support.PaymentOrder(), onlyFetchOpen: false).Single().Value;
                    var creditModel = CreditDomainModel.PreFetchForSingleCredit(creditNr, context, Support.CreditEnvSettings);
                    ocrPaymentReference = creditModel.GetOcrPaymentReference(Support.Clock.Today);
                    notificationBalance = notification.GetRemainingBalance(Support.Clock.Today);
                }

                Support.MoveToNextDayOfMonth(28);

                var nrOfPayments = (int)Math.Ceiling(InitialLoanAmount / notificationBalance) + 1;

                //Payment file
                var file = new IncomingPaymentFileWithOriginal
                {
                    OriginalFileName = "test.txt",
                    OriginalFileData = Encoding.UTF8.GetBytes("test test test"),
                    ExternalCreationDate = Support.Clock.Today,
                    ExternalId = "f207feee-057c-48d5-b48b-975d9014a8b8", //Value has no particular meaning
                    Format = "some-testfile",
                    Accounts = new List<IncomingPaymentFile.Account>
                    {
                        new IncomingPaymentFile.Account
                        {
                            AccountNr =  new IncomingPaymentFile.BankAccountNr(IBANFi.Parse("FI5040500734025283")),
                            Currency = Support.ClientConfiguration.Country.BaseCurrency,
                            DateBatches = new List<IncomingPaymentFile.AccountDateBatch>
                            {
                                new IncomingPaymentFile.AccountDateBatch
                                {
                                    BookKeepingDate = Support.Clock.Today,
                                    Payments = Enumerable.Range(1, onlyOnePayment ? 1 : nrOfPayments).Select(x => new IncomingPaymentFile.AccountDateBatchPayment
                                    {
                                        Amount = notificationBalance,
                                        OcrReference = ocrPaymentReference,
                                        ExternalId = creditNr,
                                        CustomerName = "Some name"
                                    }).ToList()
                                }
                            }
                        }
                    }
                };


                var mgr = Support.GetRequiredService<MultiCreditPlacePaymentBusinessEventManager>();

                var isOk = mgr.TryImportFile(file, true, true, out var failedMessage, out var placementMessage);
                Assert.That(isOk, Is.True);

                using (var context = Support.CreateCreditContextFactory().CreateContext())
                {
                    assert(context);
                }
            }
        }
    }
}
