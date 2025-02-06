using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using NTech.Core.Host.IntegrationTests.UlLegacy;
using NTech.Core.Module;
using NTech.Services.Infrastructure.Email;

namespace NTech.Core.Host.IntegrationTests.MlStandard
{
    public partial class ChangeTermsTests
    {
        private static void ScheduleChangeTerms(MlStandardTestRunner.TestSupport support, int pendingChangeId, string creditNr)
        {
            var mgr = support.GetRequiredService<MortgageLoansCreditTermsChangeBusinessEventManager>();

            mgr.AttachSignedAgreement(pendingChangeId, "abc123");
            var isSuccess = mgr.TryScheduleCreditTermsChange(pendingChangeId, out string failedMessage);

            using (var context = support.CreateCreditContextFactory().CreateContext())
            {
                if (!isSuccess)
                    TestContext.WriteLine(failedMessage);

                Assert.Multiple(() =>
                {
                    Assert.That(failedMessage, Is.Null);
                    Assert.That(isSuccess, Is.True);
                });
            }
        }
    }
}