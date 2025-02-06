using nCredit;
using nCredit.DbModel.BusinessEvents;
using NTech.Banking.BankAccounts.Se;
using NTech.Core.Credit.Database;
using NTech.Core.Host.IntegrationTests.UlStandard;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlStandardLikeScenarioTests
    {
        private void RepayUnplacedPayment(UlStandardTestRunner.TestSupport support, BusinessEvent unplacedPaymenEvent)
        {
            var mgr = support.GetRequiredService<RepayPaymentBusinessEventManager>();

            int unplacedPaymentId1;
            using (var context = new CreditContextExtended(support.CurrentUser, support.Clock))
            {
                unplacedPaymentId1 = context
                   .IncomingPaymentHeaders
                    .Where(x => x.Transactions.Any(y => y.BusinessEventId == unplacedPaymenEvent.Id))
                    .OrderBy(x => x.Id)
                    .Select(x => x.Id)
                    .First();
            }

            OutgoingPaymentHeader createdRepayment;
            using (var context = new CreditContextExtended(support.CurrentUser, support.Clock))
            {
                createdRepayment = context.UsingTransaction(() =>
                {
                    Assert.That(
                        mgr.TryRepay(unplacedPaymentId1, FirstUnplacedPaymentAmount, 0m, "Test customer", BankAccountNumberSe.Parse("33009608262391"), context, out var createdRepaymentLocal, out var failedMessage),
                        Is.True);

                    context.SaveChanges();

                    return createdRepaymentLocal;
                });
            }

            using (var context = new CreditContextExtended(support.CurrentUser, support.Clock))
            {
                var unplacedBalance = context
                    .TransactionsQueryable.Where(x => x.AccountCode == TransactionAccountType.UnplacedPayment.ToString())
                    .Sum(x => x.Amount);

                Assert.That(unplacedBalance, Is.EqualTo(SecondUnplacedPaymentAmount));

                createdRepayment.VerifyThat(support, sourceAccountIs: support.CreditEnvSettings.OutgoingPaymentBankAccountNr);
            }
        }
    }
}