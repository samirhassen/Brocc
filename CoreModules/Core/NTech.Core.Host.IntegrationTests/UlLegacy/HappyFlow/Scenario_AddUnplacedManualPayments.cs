using nCredit;
using nCredit.DbModel.BusinessEvents;
using NTech.Core.Credit.Database;
using NTech.Core.Host.IntegrationTests.UlLegacy;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlLegacyScenarioTests
    {
        private BusinessEvent AddUnplacedManualPayments(UlLegacyTestRunner.TestSupport support)
        {
            BusinessEvent evt;
            var mgr = new NewManualIncomingPaymentBatchBusinessEventManager(support.CurrentUser, support.Clock, support.ClientConfiguration);
            using (var context = new CreditContextExtended(support.CurrentUser, support.Clock))
            {
                evt = mgr.CreateBatch(context, new NewManualIncomingPaymentBatchBusinessEventManager.ManualPayment[]
                {
                    new NewManualIncomingPaymentBatchBusinessEventManager.ManualPayment
                    {
                        Amount = FirstUnplacedPaymentAmount,
                        BookkeepingDate = support.Clock.Today.AddDays(-1),
                        InitiatedByUserId = support.CurrentUser.UserId,
                        NoteText = "Payment1"
                    },
                    new NewManualIncomingPaymentBatchBusinessEventManager.ManualPayment
                    {
                        Amount = SecondUnplacedPaymentAmount,
                        BookkeepingDate = support.Clock.Today.AddDays(-1),
                        InitiatedByUserId = support.CurrentUser.UserId,
                        NoteText = "Payment2"
                    }
                });

                context.SaveChanges();
            }

            using (var context = new CreditContextExtended(support.CurrentUser, support.Clock))
            {
                var unplacedBalance = context
                    .TransactionsQueryable.Where(x => x.AccountCode == TransactionAccountType.UnplacedPayment.ToString())
                    .Sum(x => x.Amount);

                Assert.That(unplacedBalance, Is.EqualTo(FirstUnplacedPaymentAmount + SecondUnplacedPaymentAmount));
            }

            return evt;
        }

        public const decimal FirstUnplacedPaymentAmount = 5000m;
        public const decimal SecondUnplacedPaymentAmount = 3000m;
    }
}