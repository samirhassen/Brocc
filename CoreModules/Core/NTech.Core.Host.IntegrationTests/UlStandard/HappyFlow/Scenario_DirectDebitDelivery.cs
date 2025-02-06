using nCredit.Code.Services;
using NTech.Core.Host.IntegrationTests.UlStandard;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlStandardLikeScenarioTests
    {
        private void DirectDebitDelivery(UlStandardTestRunner.TestSupport support)
        {
            support.AssertDayOfMonth(20);

            var s = support.GetRequiredService<DirectDebitNotificationDeliveryService>();
            var errorSink = new List<string>();
            var delivery = s.CreateDelivery();
            if (delivery.ExportFile?.Notifications?.Count != 1)
            {
                TestContext.Out.WriteLine($"Skipped: {string.Join(Environment.NewLine, delivery.SkipList)}");
                TestContext.Out.WriteLine($"Errors: {string.Join(Environment.NewLine, delivery.Errors)}");
            }
            Assert.That(delivery.ExportFile?.Notifications?.Count, Is.EqualTo(1));
            Assert.That(delivery.SkipList.Count, Is.EqualTo(1)); //Credit 2 is settled so it's skipped
            Assert.That(delivery.Errors.Count, Is.EqualTo(0));
        }
    }
}
