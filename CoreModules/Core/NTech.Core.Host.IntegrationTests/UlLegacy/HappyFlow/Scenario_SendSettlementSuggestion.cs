using NTech.Core.Host.IntegrationTests.UlLegacy;
using NTech.Core.Host.IntegrationTests.UlLegacy.Utilities;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlLegacyScenarioTests
    {
        private void SendSettlementSuggestions(UlLegacyTestRunner.TestSupport support, EmailProviderState emailProviderState, int creditNumber)
        {
            support.IsEmailProviderDown = emailProviderState == EmailProviderState.Down;

            var assertThatSuccessWarningMessageContains = support.IsEmailProviderDown
                ? "Settlement suggestion created but email could not be sent."
                : null;

            var credit = CreditsUlLegacy.CreateCredit(support, creditNumber);
            var suggestion = CreditsUlLegacy.CreateAndSendSettlementSuggestion(support, credit.CreditNr,
                assertThatSuccessWarningMessageContains: assertThatSuccessWarningMessageContains);

            support.IsEmailProviderDown = false;
        }
    }
}
