using Microsoft.EntityFrameworkCore;
using nCredit;
using nCredit.DomainModel;
using NTech.Banking.BankAccounts.Se;
using NTech.Core.Credit.Database;
using NTech.Core.Host.IntegrationTests.UlStandard;
using NTech.Core.Host.IntegrationTests.UlStandard.Utilities;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlStandardLikeScenarioTests
    {
        public enum OutgoingAccountOverride
        {
            UseStandardOutgoingAccount,
            OverrideOutgoingAccount
        }

        public enum DirectDebitCode
        {
            WithDirectDebit,
            WithoutDirectDebit
        }

        private void AddCredit(UlStandardTestRunner.TestSupport support, int creditNumber, OutgoingAccountOverride overrideOutgoingAccount, DirectDebitCode directDebitCode)
        {
            var createdCredit = CreditsUlStandard.CreateCredit(support, creditNumber,
                skipOverrideOutgoingAccount: overrideOutgoingAccount == OutgoingAccountOverride.OverrideOutgoingAccount,
                activateDirectDebit: directDebitCode == DirectDebitCode.WithDirectDebit);
            var customerId = createdCredit.CreditCustomers.Single().CustomerId;

            //Expected values
            var fromBankAccountNr = BankAccountNumberSe.Parse("33000803279819");
            var withheldInitialFeeAmount = 500m;
            var settleLoanBgNr = BankGiroNumberSe.Parse("902-0033").NormalizedValue;
            var payToCustomerBankAccountNr = BankAccountNumberSe.Parse("3300190109109819").NormalizedValue;

            using (var context = new CreditContextExtended(support.CurrentUser, support.Clock))
            {
                var credit = context.CreditHeaders.Include(x => x.Transactions).Single(x => x.CreditNr == createdCredit.CreditNr);
                Assert.That(credit.CreditNr, Is.EqualTo(createdCredit.CreditNr));

                var paidToCustomerAmount = context
                    .Transactions
                    .Where(x => x.AccountCode == TransactionAccountType.ShouldBePaidToCustomer.ToString() && x.BusinessEventId == credit.CreatedByBusinessEventId)
                    .Sum(x => x.Amount);

                var domainModel = CreditDomainModel.PreFetchForSingleCredit(credit.CreditNr, context, support.CreditEnvSettings);
                Assert.That(domainModel.GetBalance(CreditDomainModel.AmountType.Capital, support.Clock.Today), Is.EqualTo(withheldInitialFeeAmount + paidToCustomerAmount));

                var settlementOutgoingPayment = context
                    .OutgoingPaymentHeaders
                    .Include(x => x.Transactions)
                    .Include(x => x.Items)
                    .Single(x => x.CreatedByBusinessEventId == createdCredit.CreatedByBusinessEventId && x.Transactions.Any(y => y.SubAccountCode == "settledLoan"));

                settlementOutgoingPayment.VerifyThat(support,
                    sourceAccountIs: fromBankAccountNr,
                    targetAccountIs: BankGiroNumberSe.Parse(settleLoanBgNr),
                    shouldBePaidToCustomerIs: 3000m);

                var paidToCustomerOutgoingPayment = context
                    .OutgoingPaymentHeaders
                    .Include(x => x.Transactions)
                    .Include(x => x.Items)
                    .Single(x => x.CreatedByBusinessEventId == createdCredit.CreatedByBusinessEventId && x.Transactions.Any(y => y.SubAccountCode == "paidToCustomer"));

                paidToCustomerOutgoingPayment.VerifyThat(support,
                    sourceAccountIs: fromBankAccountNr,
                    targetAccountIs: BankAccountNumberSe.Parse(payToCustomerBankAccountNr),
                    shouldBePaidToCustomerIs: 2000m);
            }
            Credits.ClearOutgoingPaymentAccountOverride(support);
        }
    }
}