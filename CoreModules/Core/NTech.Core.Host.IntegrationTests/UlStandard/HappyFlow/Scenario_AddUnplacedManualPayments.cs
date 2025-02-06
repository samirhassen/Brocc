using nCredit;
using NTech.Core.Credit.Database;
using NTech.Core.Host.IntegrationTests.UlStandard;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlStandardLikeScenarioTests
    {
        private BusinessEvent AddUnplacedManualPayments(UlStandardTestRunner.TestSupport support)
        {
            var evt = Credits.AddUnplacedPayments(support, support.CreditEnvSettings,
                (Amount: FirstUnplacedPaymentAmount, NoteText: "Payment1"),
                (Amount: SecondUnplacedPaymentAmount, NoteText: "Payment2"));

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