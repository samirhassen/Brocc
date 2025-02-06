using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using nCustomer.Code.Services;
using nCustomer.DbModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestsnPreCredit.ArchiveCustomers
{
    [TestClass]
    public class ArchiveCustomersTests
    {
        const int ArchivableCustomerId1 = 1;
        const int CustomerIdRejectedBySavings = 2;
        const int CustomerIdRejectedByCredit = 4;
        const int CustomerIdRejectedByRelation = 5;
        const int ArchivableCustomerId2 = 3;

        [TestMethod]
        public void VerifyGetCustomerIdsBatchToArchive()
        {
            var contextMock = new Mock<CustomerArchiveService.IArchiveContext>();
            var customerRelations = new List<CustomerRelation>
            {
                new CustomerRelation
                {
                    CustomerId = CustomerIdRejectedByRelation,
                    StartDate = new DateTime(2019, 1, 1),
                    EndDate = new DateTime(2020, 1, 1),
                    RelationId = "L42",
                    RelationType = "Credit_UnsecuredLoan"
                }
            };

            contextMock.Setup(x => x.CustomerRelations).Returns(customerRelations.AsQueryable());
            contextMock
                .Setup(x => x.GetCustomerIdsThatNCreditThinksCanBeArchived(It.IsAny<ISet<int>>()))
                .Returns(new HashSet<int>() { ArchivableCustomerId1, ArchivableCustomerId2, CustomerIdRejectedBySavings });
            contextMock
                .Setup(x => x.GetCustomerIdsThatNSavingsThinksCanBeArchived(It.IsAny<ISet<int>>()))
                .Returns(new HashSet<int>() { ArchivableCustomerId1, ArchivableCustomerId2, CustomerIdRejectedByCredit });

            AssertSetEquals(
                CustomerArchiveService.GetCustomerIdsBatchToArchive(new HashSet<int> { 1, 2, 3, 4, 5 }, contextMock.Object, "nPreCredit"),
                1, 3);

            AssertSetEquals(
                CustomerArchiveService.GetCustomerIdsBatchToArchive(new HashSet<int> { 1 }, contextMock.Object, "nPreCredit"),
                1);

            AssertSetEquals(
                CustomerArchiveService.GetCustomerIdsBatchToArchive(new HashSet<int> { 3 }, contextMock.Object, "nPreCredit"),
                3);
        }

        private static void AssertSetEquals<T>(ISet<T> actual, params T[] expected)
        {
            var expectedSet = expected.ToHashSet();
            Assert.IsTrue(expectedSet.SetEquals(actual), $"Expected: [{string.Join(", ", expectedSet)}]{Environment.NewLine}[{string.Join(", ", actual)}]");
        }
    }
}
