using Microsoft.VisualStudio.TestTools.UnitTesting;
using nCredit.DbModel.DomainModel;

namespace TestsnPreCredit.Credit
{
    [TestClass]
    public class NotificationTests
    {
        [TestMethod]
        public void ReminderFeeLogic()
        {
            var p = new NotificationProcessSettings
            {
                ReminderFeeAmount = 25,
                MaxNrOfReminders = 2,
                MaxNrOfRemindersWithFees = null,
                NrOfFreeInitialReminders = 0
            };

            Assert.AreEqual(50m, p.GetMaxTotalReminderFeePerNotification());

            p.NrOfFreeInitialReminders = 1;
            Assert.AreEqual(25m, p.GetMaxTotalReminderFeePerNotification());

            p.NrOfFreeInitialReminders = 0;
            p.MaxNrOfRemindersWithFees = 1;
            Assert.AreEqual(25m, p.GetMaxTotalReminderFeePerNotification());
        }
    }
}
