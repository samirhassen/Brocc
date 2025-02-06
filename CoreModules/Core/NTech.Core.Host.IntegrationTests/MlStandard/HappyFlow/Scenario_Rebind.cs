using nCredit;
using nCredit.DbModel.BusinessEvents;
using nCredit.DomainModel;
using NTech.Core.Host.IntegrationTests.MlStandard.Utilities;
using NTech.Core.Host.IntegrationTests.UlLegacy;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class MlStandardLikeScenarioTests
    {
        private void RebindLoan(MlStandardTestRunner.TestSupport support)
        {
            CreditDomainModel LoadCredit()
            {
                var credit = CreditsMlStandard.GetCreatedCredit(support, 1);
                using (var context = support.CreateCreditContextFactory().CreateContext())
                {
                    return CreditDomainModel.PreFetchForSingleCredit(credit!.CreditNr, context, support.CreditEnvSettings);
                }
            }

            void Day10()
            {
                CreditDomainModel credit = LoadCredit();
                Assert.That(credit.GetDatedCreditValueOpt(support.Clock.Today, DatedCreditValueCode.MortgageLoanInterestRebindMonthCount), Is.EqualTo(3m));

                var changeTermsServices = CreditsMlStandard.CreateChangeTermsServices(support);
                var mgr = changeTermsServices.Manager;
                var newTerms = new MlNewChangeTerms
                {
                    NewFixedMonthsCount = 4,
                    NewInterestBoundFrom = support.Clock.Today,
                    NewMarginInterestRatePercent = 3m,
                    //TODO: Remove this from the change model since it's completely ignored by the logic anyway
                    NewReferenceInterestRatePercent = 1m
                };
                var customerClient = TestPersons.CreateRealisticCustomerClient(support).Object;
                mgr.TryComputeMlTermsChangeData(credit.CreditNr, newTerms, out MlTermsChangeData termsChangeData, out var failedMessage).AssertTrue(failedMessage);
                var startResult = mgr.MlStartCreditTermsChange(credit.CreditNr, newTerms, () => Credits.CreateDocumentRenderer(), customerClient);
                startResult.IsSuccess.AssertTrue("Failed to start term change");
                mgr.AttachSignedAgreement(startResult.TermChange.Id, "abc123");
                mgr.TryScheduleCreditTermsChange(startResult.TermChange.Id, out var failedMessage2).AssertTrue(failedMessage2);
            }

            void Day11()
            {
                CreditDomainModel credit = LoadCredit();
                Assert.Multiple(() =>
                {
                    Assert.That(credit.GetInterestRatePercent(support.Clock.Today), Is.EqualTo(3m + 1m));
                    Assert.That(credit.GetDatedCreditValue(support.Clock.Today, DatedCreditValueCode.MortgageLoanInterestRebindMonthCount), Is.EqualTo(4m));
                });
            }

            CreditsMlStandard.RunOneMonth(support, beforeDay: x =>
            {
                if (x == 10)
                    Day10();
                else if (x == 11)
                    Day11();
            });
        }
    }
}