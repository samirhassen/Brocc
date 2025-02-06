using NTech.Core.Host.IntegrationTests.UlLegacy;
using NTech.Core.Host.IntegrationTests.UlLegacy.Utilities;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlLegacyScenarioTests
    {
        public enum EmailProviderState
        {
            Down,
            Working
        }

        private void ChangeTermsOnCredit(UlLegacyTestRunner.TestSupport support, EmailProviderState emailProviderState, int creditNumber)
        {
            support.IsEmailProviderDown = emailProviderState == EmailProviderState.Down;

            var expectedSuccessWarningMessage = emailProviderState == EmailProviderState.Down
                ? "Change terms - agreement created but email could not be sent."
                : null;
            var expectedCommentPart = emailProviderState == EmailProviderState.Down
                ? "but email could not be sent"
                : "and email sent";

            var createdCredit = CreditsUlLegacy.CreateCredit(support, creditNumber);
            CreditsUlLegacy.StartChangeTerms(support, createdCredit.CreditNr, 20, 12m, isEmailProviderDown: emailProviderState == EmailProviderState.Down,
                assertSuccessWarningMessage: expectedSuccessWarningMessage,
                assertSystemCommentContains: expectedCommentPart);

            support.IsEmailProviderDown = false;
        }
    }
}