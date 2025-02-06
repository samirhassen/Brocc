using nCredit;
using nCredit.DbModel.BusinessEvents;
using NTech.Banking.BankAccounts.Se;
using NTech.Core.Credit.Database;
using NTech.Core.Host.IntegrationTests.UlStandard;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlStandardLikeScenarioTests
    {
        private void RepayUnplacedPayment_OverrideSourceAccountWithSetting(UlStandardTestRunner.TestSupport support, BusinessEvent unplacedPaymenEvent)
        {
            var paymentAccountService = support.CreatePaymentAccountService(support.CreditEnvSettings);
            var mgr = support.GetRequiredService<RepayPaymentBusinessEventManager>();

            int unplacedPaymentId2;
            using (var context = new CreditContextExtended(support.CurrentUser, support.Clock))
            {
                unplacedPaymentId2 = context
                   .IncomingPaymentHeaders
                    .Where(x => x.Transactions.Any(y => y.BusinessEventId == unplacedPaymenEvent.Id))
                    .OrderBy(x => x.Id)
                    .Select(x => x.Id)
                    .Skip(1)
                    .First();
            }

            var settingsService = support.CreateSettingsService();

            var outgoingPaymentSourceBankAccount = BankAccountNumberSe.Parse("33000803279819");
            Credits.OverrideOutgoingPaymentAccount(outgoingPaymentSourceBankAccount, support);

            OutgoingPaymentHeader createdRepayment;
            using (var context = new CreditContextExtended(support.CurrentUser, support.Clock))
            {
                createdRepayment = context.UsingTransaction(() =>
                {
                    Assert.That(
                        mgr.TryRepay(unplacedPaymentId2, SecondUnplacedPaymentAmount, 0m, "Test customer", BankAccountNumberSe.Parse("33009608262391"), context, out createdRepayment, out var failedMessage),
                        Is.True);

                    context.SaveChanges();

                    return createdRepayment;
                });
            }

            using (var context = new CreditContextExtended(support.CurrentUser, support.Clock))
            {
                var unplacedBalance = context
                    .TransactionsQueryable.Where(x => x.AccountCode == TransactionAccountType.UnplacedPayment.ToString())
                    .Sum(x => x.Amount);

                Assert.That(unplacedBalance, Is.EqualTo(0m));

                createdRepayment.VerifyThat(support, sourceAccountIs: outgoingPaymentSourceBankAccount);
            }

            Credits.ClearOutgoingPaymentAccountOverride(support);
        }
    }
}