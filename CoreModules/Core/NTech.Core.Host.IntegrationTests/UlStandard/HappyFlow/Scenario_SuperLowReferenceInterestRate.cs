using nCredit.DbModel.BusinessEvents;
using nCredit.DomainModel;
using NTech.Core.Host.IntegrationTests.UlStandard;
using NTech.Core.Host.IntegrationTests.UlStandard.Utilities;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlStandardLikeScenarioTests
    {
        private void SetAndRemoveSuperLowReferenceInterestRate(UlStandardTestRunner.TestSupport support, int creditNumber)
        {
            var creditNr = CreditsUlStandard.GetCreateCredit(support, creditNumber).CreditNr;

            var r = new ReferenceInterestRateChangeBusinessEventManager(support.CurrentUser, new nCredit.Code.Services.LegalInterestCeilingService(support.CreditEnvSettings), support.CreditEnvSettings, support.Clock, support.ClientConfiguration);
            using (var context = support.CreateCreditContextFactory().CreateContext())
            {
                var creditBefore = CreditDomainModel.PreFetchForSingleCredit(creditNr, context, support.CreditEnvSettings);

                var refRateBefore = creditBefore.GetDatedCreditValue(support.Clock.Today, nCredit.DatedCreditValueCode.ReferenceInterestRate);
                Assert.IsTrue(r.TryChangeReferenceInterest(context, -99, out var _, out var failedMessage), failedMessage);
                context.SaveChanges();

                var creditAfter = CreditDomainModel.PreFetchForSingleCredit(creditNr, context, support.CreditEnvSettings);
                Assert.That(creditAfter.GetInterestRatePercent(support.Clock.Today), Is.EqualTo(0m));

                Assert.IsTrue(r.TryChangeReferenceInterest(context, refRateBefore, out var _, out var __));
                context.SaveChanges();
            }
        }
    }
}