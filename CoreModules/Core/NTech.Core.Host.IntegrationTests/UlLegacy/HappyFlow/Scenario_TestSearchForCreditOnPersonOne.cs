using Moq;
using nCredit.Code.Services;
using NTech.Banking.CivicRegNumbers;
using NTech.Core.Credit.Database;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.UlLegacy;
using ICustomerClient = NTech.Core.Module.Shared.Clients.ICustomerClient;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlLegacyScenarioTests
    {
        private void TestSearchForCreditOnPersonOne(UlLegacyTestRunner.TestSupport support)
        {
            var customerClient = new Mock<ICustomerClient>(MockBehavior.Strict);
            var creditContextFactory = new CreditContextFactory(() => new CreditContextExtended(support.CurrentUser, support.Clock));
            var service = new CreditSearchService(customerClient.Object, support.ClientConfiguration, creditContextFactory,
                support.CreditEnvSettings);

            var customerOneCustomerId = TestPersons.GetTestPersonCustomerIdBySeed(support, 1);
            var creditNr = (string)support.Context["TestPerson1_CreditNr"];
            var civicRegNr = TestPersons.GetTestPersonDataBySeed(support, 1)["civicRegNr"];
            customerClient
                .Setup(x => x.GetCustomerId(It.IsAny<ICivicRegNumber>()))
                .Returns<ICivicRegNumber>(x =>
                {
                    if (x.NormalizedValue == civicRegNr)
                        return customerOneCustomerId;
                    else
                        throw new Exception("Unexpected customer: " + x.NormalizedValue);
                });
            customerClient.Setup(x => x.FindCustomerIdsOmni(It.IsAny<string>())).Returns<string>(searchQuery =>
            {
                if (searchQuery == civicRegNr)
                    return new List<int> { customerOneCustomerId };
                else
                    return new List<int>();
            });

            var creditSearchResult = service.Search(new SearchCreditRequest { OmnisearchValue = creditNr }).Single();
            Assert.That(creditSearchResult.ConnectedCustomerIds, Contains.Item(customerOneCustomerId));
            Assert.That(creditSearchResult.CreditNr, Is.EqualTo(creditNr));

            creditSearchResult = service.Search(new SearchCreditRequest { OmnisearchValue = civicRegNr }).Single();
            Assert.That(creditSearchResult.ConnectedCustomerIds, Contains.Item(customerOneCustomerId));
            Assert.That(creditSearchResult.CreditNr, Is.EqualTo(creditNr));

            var creditCustomerSearchService = new CreditCustomerSearchSourceService(service, creditContextFactory, support.Clock);
            Assert.That(creditCustomerSearchService.FindCustomers(creditNr), Contains.Item(customerOneCustomerId));
            var customerEntities = creditCustomerSearchService.GetCustomerEntities(customerOneCustomerId);
            Assert.True(customerEntities.Any(x => x.EntityId == creditNr && x.Customers.Any(y => y.CustomerId == customerOneCustomerId && y.Roles.Contains("creditCustomer"))));
        }

    }
}