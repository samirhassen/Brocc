using NTech.Core.Host.IntegrationTests.MlStandard.Utilities;
using NTech.Core.Host.IntegrationTests.UlLegacy;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class MlStandardLikeScenarioTests
    {
        [Test]
        public void TestHappyFlow1()
        {
            MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                support.Now = new DateTime(2022, 1, 1);
                AddLoan(support);
                RevalueLoan(support);
                RebindLoan(support);
                BindExpirationReminder(support);
                SettleLoan(support);
            });
        }

        private void DoAfterDay(MlStandardTestRunner.TestSupport support, int dayNr, Action a)
        {
            CreditsMlStandard.RunOneMonth(support, beforeDay: x =>
            {
                if (x == dayNr)
                {
                    a();
                }
            }, payNotificationsOnDueDate: true);
        }
    }
}