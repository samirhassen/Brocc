using Microsoft.VisualStudio.TestTools.UnitTesting;
using nPreCredit.WebserviceMethods;
using System;
using static nPreCredit.WebserviceMethods.FilterOutCustomersWithInactiveApplicationsMethod;

namespace TestsnPreCredit.ArchiveCreditReports
{
    [TestClass]
    public class ArchiveCreditReportsTests
    {
        [TestMethod]
        public void VerifyActiveCreditReportCustomerConnectionIsNotArchived()
        {
            var minNrOfDaysInactive = 90;
            var activeApplication = CreateApplicationModel();
            activeApplication.IsActive = true;

            //True => application is not archived 
            Assert.IsTrue(
                FilterOutCustomersWithInactiveApplicationsMethod.DoesApplicationVetoArchiving(activeApplication, DateTime.Now, minNrOfDaysInactive)
                );
        }

        [TestMethod]
        public void VerifyActiveApplicationWithNewFinalDecisionDateIsNotArchived()
        {
            var minNrOfDaysInactive = 90;
            var activeApplicationWithFinalDecisionDate = CreateApplicationModel();
            activeApplicationWithFinalDecisionDate.FinalDecisionDate = GetXDaysAgoDateTime(10);

            //True => application is not archived 
            Assert.IsTrue(
                FilterOutCustomersWithInactiveApplicationsMethod.DoesApplicationVetoArchiving(activeApplicationWithFinalDecisionDate, DateTime.Now, minNrOfDaysInactive)
                );
        }

        [TestMethod]
        public void VerifyInactiveApplicationWithOldCancelledDateIsArchived()
        {
            var minNrOfDaysInactive = 90;
            var inactiveCancelledDateApplication = CreateApplicationModel();
            inactiveCancelledDateApplication.CancelledDate = GetXDaysAgoDateTime(150);

            //False => application is archived 
            Assert.IsFalse(
                FilterOutCustomersWithInactiveApplicationsMethod.DoesApplicationVetoArchiving(inactiveCancelledDateApplication, DateTime.Now, minNrOfDaysInactive)
                );
        }

        [TestMethod]
        public void VerifyInactiveApplicationWithOldRejectedDateIsArchived()
        {
            var minNrOfDaysInactive = 90;
            var inactiveRejectedDateApplication = CreateApplicationModel();
            inactiveRejectedDateApplication.RejectedDate = GetXDaysAgoDateTime(200);

            //False => application is archived 
            Assert.IsFalse(
                FilterOutCustomersWithInactiveApplicationsMethod.DoesApplicationVetoArchiving(inactiveRejectedDateApplication, DateTime.Now, minNrOfDaysInactive)
                );
        }

        public DateTimeOffset? GetXDaysAgoDateTime(int nrOfDaysAgo)
        {
            return new DateTimeOffset(DateTime.Now.Subtract(TimeSpan.FromDays(nrOfDaysAgo)));
        }

        public ApplicationModel CreateApplicationModel()
        {
            return new ApplicationModel
            {
                CustomerId = "1",
                IsActive = false,
                FinalDecisionDate = null,
                RejectedDate = null,
                CancelledDate = null,
                ChangedDate = null
            };
        }
    }
}
