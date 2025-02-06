using nCredit;
using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using NTech.Core.Host.IntegrationTests.UlLegacy;

namespace NTech.Core.Host.IntegrationTests.MlStandard
{
    public partial class ChangeTermsTests
    {
        private static void TooHighMarginInterestRate(MlStandardTestRunner.TestSupport support, CreditHeader credit, decimal loanAmount, decimal referenceInterestRatePercent, int newInterestBoundUntilMonths, decimal newMarginInterestRatePercent)
        {
            var mgr = support.GetRequiredService<MortgageLoansCreditTermsChangeBusinessEventManager>();

            var newTerms = new MlNewChangeTerms
            {
                NewFixedMonthsCount = newInterestBoundUntilMonths,
                NewMarginInterestRatePercent = newMarginInterestRatePercent,
                NewReferenceInterestRatePercent = referenceInterestRatePercent
            };

            var isSuccess = mgr.TryComputeMlTermsChangeData(credit.CreditNr, newTerms, out MlTermsChangeData tc, out string failedMessage);
            Assert.Multiple(() =>
            {
                Assert.That(isSuccess, Is.False);
                Assert.That(failedMessage, Is.EqualTo("Margin interest rate outside the allowed range."));
            });
        }
    }
}