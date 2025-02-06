using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using NTech.Core.Host.IntegrationTests.UlLegacy;

namespace NTech.Core.Host.IntegrationTests.MlStandard
{
    public partial class ChangeTermsTests
    {
        private static void CompleteChangeTerms(MlStandardTestRunner.TestSupport support, string creditNr, bool isDefault)
        {
            var mgr = support.GetRequiredService<MortgageLoansCreditTermsChangeBusinessEventManager>();

            var updatedChangeTerms = mgr.UpdateChangeTerms();

            Assert.Multiple(() =>
            {
                var list = isDefault ? updatedChangeTerms.UpdatedDefault : updatedChangeTerms.UpdatedPendingChange;
                Assert.That(list, Has.Count.EqualTo(1));
                Assert.That(list.First(), Is.EqualTo(creditNr));
            });
        }
    }
}