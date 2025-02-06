using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.UlLegacy.Utilities;
using NTech.Core.PreCredit.Shared.Code.PetrusOnlyScoringService;

namespace NTech.Core.Host.IntegrationTests.UlLegacy
{
    public class PetrusTwoScoringTests
    {
        [Test]
        public void TestPetrus2()
        {
            UlLegacyTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                Accepted(support);
                Rejected(support);
            });
        }

        private void Accepted(UlLegacyTestRunner.TestSupport support)
        {
            var applicationNr = ApplicationsLegacy.CreateApplication(support, 1, 
                requestedRepaymentTimeInYears: 4, 
                requestedAmount: 9580);

            var result = ApplicationsLegacy.DoAutomaticCreditCheckOnApplication_Accept(support, applicationNr, new PetrusOnlyCreditCheckResponse.OfferModel
            {
                Amount = 2000m,
                InitialFeeAmount = 100m,
                NotificationFeeAmount = 12m,
                MarginInterestRatePercent = 15m,
                RepaymentTimeInMonths = 36
            });

            Assert.That(result?.IsAccepted, Is.True);

            var offer = result.Value;
            Assert.Multiple(() =>
            {
                Assert.That(offer.OfferedInterestRate, Is.EqualTo(15m));
                Assert.That(offer.OfferedRepaymentTimeInMonths, Is.EqualTo(36));
                Assert.That(offer.OfferedAmount, Is.EqualTo(2000m));

            });
        }

        private void Rejected(UlLegacyTestRunner.TestSupport support)
        {
            var applicationNr = ApplicationsLegacy.CreateApplication(support, 2,
                requestedRepaymentTimeInYears: 4,
                requestedAmount: 9580);

            MockPetrusOnlyScoringService.ResultOverride = (IsAccepted: false, AcceptedOffer: null);
            var result = ApplicationsLegacy.DoAutomaticCreditCheckOnApplication_Reject(support, applicationNr, "score");

            Assert.That(result?.IsAccepted, Is.False);

            var rejectionReasons = result.Value.RejectionReasons;
            Assert.Multiple(() =>
            {
                Assert.That(rejectionReasons, Has.Member("score"));
            });
        }
    }
}
