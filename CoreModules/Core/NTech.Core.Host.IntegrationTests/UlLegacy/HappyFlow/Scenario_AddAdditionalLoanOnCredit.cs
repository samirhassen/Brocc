using nCredit;
using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using nCredit.DbModel.BusinessEvents.NewCredit;
using NTech.Banking.BankAccounts;
using NTech.Banking.BankAccounts.Fi;
using NTech.Core.Credit.Database;
using NTech.Core.Host.IntegrationTests.UlLegacy;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlLegacyScenarioTests
    {
        private void AddAdditionalLoanOnCredit(UlLegacyTestRunner.TestSupport support)
        {
            var creditNr = (string)support.Context["TestPerson1_CreditNr"];

            decimal capitalDebtBefore;
            using (var context = new CreditContextExtended(support.CurrentUser, support.Clock))
            {
                var credit = Credits.LoadCreditForTesting(creditNr, context);
                capitalDebtBefore = credit.GetBalanceForTesting(TransactionAccountType.CapitalDebt);
            }

            var customerId = TestPersons.GetTestPersonCustomerIdBySeed(support, 1);

            var legalInterestCeilingService = new LegalInterestCeilingService(support.CreditEnvSettings);

            var customerClient = TestPersons.CreateRealisticCustomerClient(support);

            var paymentAccountService = support.CreatePaymentAccountService(support.CreditEnvSettings);
            var mgr = new NewAdditionalLoanBusinessEventManager(support.CurrentUser, legalInterestCeilingService, support.CreditEnvSettings, customerClient.Object, support.Clock, support.ClientConfiguration,
                support.EncryptionService, paymentAccountService);

            IBankAccountNumber payToBankAccountNr = IBANFi.Parse("FI9340534400707976");
            NewAdditionalLoanRequest additionalLoanRequest;
            BusinessEvent additionalLoanEvent;
            using (var context = new CreditContextExtended(support.CurrentUser, support.Clock))
            {
                additionalLoanRequest = new NewAdditionalLoanRequest
                {
                    AdditionalLoanAmount = 2000m,
                    Iban = payToBankAccountNr.FormatFor(null),
                    NewAnnuityAmount = 250m,
                    NewMarginInterestRatePercent = 8m,
                    CreditNr = creditNr,
                    ProviderName = "self"
                };
                additionalLoanEvent = context.UsingTransaction(() =>
                {
                    var evt = mgr.CreateNew(context, additionalLoanRequest);
                    context.SaveChanges();
                    return evt;
                });
            }

            using (var context = new CreditContextExtended(support.CurrentUser, support.Clock))
            {
                var outgoingPayment = Credits.LoadOutgoingPaymentForTesting(
                    context,
                    businessEventId: additionalLoanEvent.Id);

                outgoingPayment.VerifyThat(support,
                    sourceAccountIs: support.CreditEnvSettings.OutgoingPaymentBankAccountNr,
                    targetAccountIs: payToBankAccountNr,
                    shouldBePaidToCustomerIs: additionalLoanRequest.AdditionalLoanAmount);

                var credit = Credits.LoadCreditForTesting(creditNr, context);
                credit.VerifyThat(support, capitalDebtIs: capitalDebtBefore + additionalLoanRequest.AdditionalLoanAmount);
            }
        }
    }
}