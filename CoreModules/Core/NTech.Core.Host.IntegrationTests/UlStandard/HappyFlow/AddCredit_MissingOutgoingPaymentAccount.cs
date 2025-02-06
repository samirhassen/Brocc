using NTech.Core.Host.IntegrationTests.UlStandard;
using NTech.Core.Module.Shared.Infrastructure;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlStandardLikeScenarioTests
    {
        private void AddCredit_MissingOutgoingPaymentAccount(UlStandardTestRunner.TestSupport support)
        {
            var account = support.OutgoingPaymentAccount;
            support.OutgoingPaymentAccount = null!;
            try
            {
                AddCredit(support, 2, OutgoingAccountOverride.OverrideOutgoingAccount, DirectDebitCode.WithoutDirectDebit);
                Assert.Fail("Should fail with NTechCoreWebserviceException, errorCode = missingOutgoingPaymentAccount");
            }
            catch (NTechCoreWebserviceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo("missingOutgoingPaymentAccount"));
            }
            finally
            {
                support.OutgoingPaymentAccount = account;
            }
        }
    }
}