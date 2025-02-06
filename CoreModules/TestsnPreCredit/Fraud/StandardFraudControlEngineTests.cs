using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using nPreCredit;
using nPreCredit.Code;
using NTech.Core.Module.Shared.Clients;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace TestsnPreCredit.Fraud
{
    [TestClass]
    public class StandardFraudControlEngineTests
    {

        private StandardFraudControlEngine _engine;

        [TestMethod]
        public void SameAddressCheck_OneCustomerWithMatchingAddress_ShouldReturnMatchingApplicationNr()
        {
            var applicationNrToRunChecksFor = "UL10001";
            var matchingApplicationNr = "UL55555";
            var matchingCustomerId = 1002;

            var customerClient = new Mock<ICustomerClient>();
            customerClient
                .Setup(x => x.GetCustomerIdsWithSameAdress(It.IsAny<int>(), It.IsAny<bool>()))
                .Returns(new List<int> { matchingCustomerId });

            var items = new List<ComplexApplicationListItem>();
            items.AddRange(ApplicationItemFactory.Create().WithApplicants(applicationNrToRunChecksFor, 1001));
            items.AddRange(ApplicationItemFactory.Create().WithApplicants(matchingApplicationNr, matchingCustomerId, 1003));
            items.AddRange(ApplicationItemFactory.Create().WithApplicants("UL33333", 1004, 1004));

            var mockedItems = GetMockedComplexApplicationListItemDbSet(items);

            var context = new Mock<IPreCreditContext>();
            context.Setup(x => x.ComplexApplicationListItems).Returns(mockedItems.Object);

            _engine = new StandardFraudControlEngine(customerClient.Object, context.Object);
            var result = _engine.RunFraudChecks(applicationNrToRunChecksFor, new List<string> { "SameAddressCheck" });

            Assert.AreEqual(matchingApplicationNr, result.FraudControls.Single(x => x.CheckName == "SameAddressCheck").Values.Single());
        }

        [TestMethod]
        public void SameEmailCheck_SameEmailOnBothApplicantsOnTheSameApplication_ShouldNotReturnMatch()
        {
            var applicationNrToRunChecksFor = "UL10001";

            // Both customers has the same email. 
            var customerClient = new Mock<ICustomerClient>();
            customerClient
                .Setup(x => x.BulkFetchPropertiesByCustomerIdsD(It.IsAny<ISet<int>>(), It.IsAny<string>()))
                .Returns(new Dictionary<int, Dictionary<string, string>>
                {
                    {1001, new Dictionary<string, string>() { {"email", "sameemail@example.com"} }},
                    {1002, new Dictionary<string, string>() { {"email", "sameemail@example.com" } }}
                });

            // We get both customerids back with this check. 
            customerClient
                .Setup(x => x.GetCustomerIdsWithSameData("email", It.IsAny<string>()))
                .Returns(new List<int> { 1001, 1002 });

            var items = new List<ComplexApplicationListItem>();
            items.AddRange(ApplicationItemFactory.Create().WithApplicants(applicationNrToRunChecksFor, 1001, 1002));
            items.AddRange(ApplicationItemFactory.Create().WithApplicants("UL10301", 1003, 1004));
            items.AddRange(ApplicationItemFactory.Create().WithApplicants("UL12345", 1005));

            var context = new Mock<IPreCreditContext>();
            var mockedItems = GetMockedComplexApplicationListItemDbSet(items);

            context.Setup(x => x.ComplexApplicationListItems).Returns(mockedItems.Object);

            _engine = new StandardFraudControlEngine(customerClient.Object, context.Object);
            var result = _engine.RunFraudChecks(applicationNrToRunChecksFor, new List<string> { "SameEmailCheck" });

            Assert.IsFalse(result.FraudControls.Single(x => x.CheckName == "SameEmailCheck").HasMatch);
        }

        [TestMethod]
        public void SameEmailCheck_SameCustomerOnTwoApplications_ShouldNotShowMatchOnItself()
        {
            var applicationNrToRunChecksFor = "UL10001";
            var sameCustomerId = 1002;

            // Both customers has the same email. 
            var customerClient = new Mock<ICustomerClient>();
            customerClient
                .Setup(x => x.BulkFetchPropertiesByCustomerIdsD(It.IsAny<ISet<int>>(), It.IsAny<string>()))
                .Returns(new Dictionary<int, Dictionary<string, string>>
                {
                    {1001, new Dictionary<string, string>() { {"email", "sameemail@example.com"} }},
                    {1002, new Dictionary<string, string>() { {"email", "sameemail@example.com" } }}
                });

            // We get both customerids back with this check. 
            customerClient
                .Setup(x => x.GetCustomerIdsWithSameData("email", It.IsAny<string>()))
                .Returns(new List<int> { 1001, sameCustomerId });

            var items = new List<ComplexApplicationListItem>();
            items.AddRange(ApplicationItemFactory.Create().WithApplicants(applicationNrToRunChecksFor, 1001, sameCustomerId));
            items.AddRange(ApplicationItemFactory.Create().WithApplicants("UL10301", sameCustomerId, 1003));
            items.AddRange(ApplicationItemFactory.Create().WithApplicants("UL12345", 1004));

            var context = new Mock<IPreCreditContext>();
            var mockedItems = GetMockedComplexApplicationListItemDbSet(items);

            context.Setup(x => x.ComplexApplicationListItems).Returns(mockedItems.Object);

            _engine = new StandardFraudControlEngine(customerClient.Object, context.Object);
            var result = _engine.RunFraudChecks(applicationNrToRunChecksFor, new List<string> { "SameEmailCheck" });

            Assert.IsFalse(result.FraudControls.Single(x => x.CheckName == "SameEmailCheck").HasMatch);
        }

        private Mock<DbSet<ComplexApplicationListItem>> GetMockedComplexApplicationListItemDbSet(List<ComplexApplicationListItem> items)
        {
            var asQueryable = items.AsQueryable();
            var mockedDbSet = new Mock<DbSet<ComplexApplicationListItem>>();

            mockedDbSet.As<IQueryable<ComplexApplicationListItem>>().Setup(m => m.Provider).Returns(asQueryable.Provider);
            mockedDbSet.As<IQueryable<ComplexApplicationListItem>>().Setup(m => m.Expression).Returns(asQueryable.Expression);
            mockedDbSet.As<IQueryable<ComplexApplicationListItem>>().Setup(m => m.ElementType).Returns(asQueryable.ElementType);
            mockedDbSet.As<IQueryable<ComplexApplicationListItem>>().Setup(m => m.GetEnumerator()).Returns(asQueryable.GetEnumerator());

            return mockedDbSet;
        }

        private ComplexApplicationListItem CreateApplicationItem(string applicationNr, string listName, int nr, string itemName, string itemValue)
        {
            return new ComplexApplicationListItem
            {
                ApplicationNr = applicationNr,
                ListName = listName,
                Nr = nr,
                ItemName = itemName,
                ItemValue = itemValue
            };
        }

    }

    public static class ApplicationItemFactory
    {

        public static List<ComplexApplicationListItem> Create()
        {
            return new List<ComplexApplicationListItem>();
        }

        public static List<ComplexApplicationListItem> WithApplicants(this List<ComplexApplicationListItem> items, string applicationNr, params int[] customerIds)
        {
            var nr = 1;
            foreach (var id in customerIds)
            {
                items.Add(CreateApplicationItem(applicationNr, "Applicant", nr, "customerId", id.ToString()));
                nr++;
            }
            return items;
        }

        public static List<ComplexApplicationListItem> WithPaidToCustomerAccount(this List<ComplexApplicationListItem> items, string applicationNr, string bankAccountNr)
        {
            items.Add(CreateApplicationItem(applicationNr, "FinalLoanTerms", 1, "paidToCustomerBankAccountNr", bankAccountNr));
            return items;
        }

        private static ComplexApplicationListItem CreateApplicationItem(string applicationNr, string listName, int nr, string itemName, string itemValue)
        {
            return new ComplexApplicationListItem
            {
                ApplicationNr = applicationNr,
                ListName = listName,
                Nr = nr,
                ItemName = itemName,
                ItemValue = itemValue
            };
        }

    }

}
