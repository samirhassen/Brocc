using nCredit;
using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using NTech.Core.Host.IntegrationTests.UlLegacy;
using NTech.Core.Module;
using NTech.Services.Infrastructure.Email;

namespace NTech.Core.Host.IntegrationTests.MlStandard
{
    public partial class ChangeTermsTests
    {
        private static void ComputeNewTerms(MlStandardTestRunner.TestSupport support, CreditHeader credit, decimal loanAmount, MlNewChangeTerms newTerms)
        {
            var mgr = support.GetRequiredService<MortgageLoansCreditTermsChangeBusinessEventManager>();

            var isSuccess = mgr.TryComputeMlTermsChangeData(credit.CreditNr, newTerms, out MlTermsChangeData tc, out string failedMessage);

            if (!isSuccess)
                TestContext.WriteLine(failedMessage);

            Assert.Multiple(() =>
            {
                Assert.That(isSuccess, Is.True, $"Succces: {failedMessage}");
                Assert.That(tc?.CustomersNewTotalInterest, Is.EqualTo(newTerms.NewReferenceInterestRatePercent + newTerms.NewMarginInterestRatePercent), "CustomersNewTotalInterest");
                Assert.That(tc?.ReferenceInterest, Is.EqualTo(newTerms.NewReferenceInterestRatePercent), "ReferenceInterest");
                Assert.That(tc?.NewInterestRebindMonthCount, Is.EqualTo(newTerms.NewFixedMonthsCount), "NewInterestRebindMonthCount");
                Assert.That(tc?.MarginInterest, Is.EqualTo(newTerms.NewMarginInterestRatePercent), "MarginInterest");
                Assert.That(tc?.CurrentCapitalBalance, Is.EqualTo(loanAmount), "CurrentCapitalBalance");
            });
        }
    }
}