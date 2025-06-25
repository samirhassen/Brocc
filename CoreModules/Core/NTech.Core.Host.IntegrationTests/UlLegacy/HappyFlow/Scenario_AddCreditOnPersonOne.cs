using Microsoft.EntityFrameworkCore;
using nCredit;
using nCredit.DbModel.BusinessEvents;
using nCredit.DomainModel;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.Shared.BankAccounts.Fi;
using NTech.Core.Credit.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.UlLegacy;
using NTech.Core.Host.IntegrationTests.UlLegacy.Utilities;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlLegacyScenarioTests
    {
        private void AddCreditOnPersonOne(UlLegacyTestRunner.TestSupport support)
        {
            var applicationNr = (string)support.Context["TestPerson1_ApplicationNr"];
            var customerId = TestPersons.GetTestPersonCustomerIdBySeed(support, 1);
            var expectedCreditAmount = 6000m;
            var createdCredit = CreditsUlLegacy.CreateCredit(support, 1, mainApplicantCustomerId: customerId, applicationNr: applicationNr, creditAmount: expectedCreditAmount);
            support.Context["TestPerson1_CreditNr"] = createdCredit.CreditNr;

            using (var context = new CreditContextExtended(support.CurrentUser, support.Clock))
            {
                var credit = context.CreditHeaders.Include(x => x.Transactions).Single(x => x.CreditNr == createdCredit.CreditNr);

                var actualCapitalBalance = credit.GetBalanceForTesting(TransactionAccountType.CapitalDebt);
                Assert.That(actualCapitalBalance, Is.EqualTo(expectedCreditAmount));

                var domainModel = CreditDomainModel.PreFetchForSingleCredit(credit.CreditNr, context, support.CreditEnvSettings);
                Assert.That(actualCapitalBalance, Is.EqualTo(domainModel.GetBalance(CreditDomainModel.AmountType.Capital, support.Clock.Today)));

                var outgoingPayment = Credits.LoadOutgoingPaymentForTesting(context, businessEventId: createdCredit.CreatedByBusinessEventId);

                outgoingPayment.VerifyThat(support,
                    sourceAccountIs: support.CreditEnvSettings.OutgoingPaymentBankAccountNr,
                    targetAccountIs: IBANFi.Parse("FI4840541519568274"),
                    shouldBePaidToCustomerIs: expectedCreditAmount);
            }

            //Check that the pending settlement check works even with no settlment exists (added to cover a bug that broke this previously)
            using (var context = new CreditContextExtended(support.CurrentUser, support.Clock))
            {
                var mgr = support.GetRequiredService<CreditSettlementSuggestionBusinessEventManager>();
                var settlement = mgr.GetPendingSettlementIfAny(createdCredit.CreditNr, context);
                Assert.That(settlement, Is.Null);
            }
        }
    }
}