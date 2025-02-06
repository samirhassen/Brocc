using Newtonsoft.Json;
using nSavings;
using nSavings.DbModel.BusinessEvents;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Savings.Shared.Database;

namespace NTech.Core.Host.IntegrationTests.UlLegacy.Savings;

public partial class SavingsFiHappyFlowTests
{
    internal void ExportAccountToCustoms(UlLegacyTestRunner.TestSupport support)
    {
        void ExportWithAssert(int expectedFileCount, int expectedCustomerCount, int expectedAccountCount)
        {
            var exportedModels = SavingsTestUtils.ExportToCustoms(support);
            
            try
            {
                Assert.That(exportedModels.Count, Is.EqualTo(expectedFileCount), "Customs model count");
                if (expectedFileCount == 0) return;
                var m = exportedModels.Single();
                Assert.That(m.Customers.Count, Is.EqualTo(expectedCustomerCount), "Customer count");
                Assert.That(m.SavingsAccounts.Count, Is.EqualTo(expectedAccountCount), "Account count");
            }
            catch
            {
                Console.WriteLine(JsonConvert.SerializeObject(exportedModels, Formatting.Indented));
                throw;
            }
        }

        //New account exported
        ExportWithAssert(expectedFileCount: 1, expectedCustomerCount: 1, expectedAccountCount: 1);

        //Second export, no changes, nothing exported
        ExportWithAssert(expectedFileCount: 0, expectedCustomerCount: 0, expectedAccountCount: 0);

        var service = new FakeCloseAccountManager(support.CurrentUser, support.Clock, support.ClientConfiguration, 
            SavingsTestUtils.CreateContextFactory());
        service.CloseAccount("S20030");

        //Account closed but no customer data changed
        ExportWithAssert(expectedFileCount: 1, expectedCustomerCount: 0, expectedAccountCount: 1);
    }

    //TODO: Replace with AccountClosureBusinessEventManager when that is migrated to core
    private class FakeCloseAccountManager : BusinessEventManagerBaseCore
    {
        private readonly SavingsContextFactory contextFactory;

        public FakeCloseAccountManager(INTechCurrentUserMetadata currentUser, ICoreClock clock, IClientConfigurationCore clientConfiguration, SavingsContextFactory contextFactory) : base(currentUser, clock, clientConfiguration)
        {
            this.contextFactory = contextFactory;
        }

        public void CloseAccount(string savingsAccountNr)
        {
            using(var context = contextFactory.CreateContext())
            {
                var evt = AddBusinessEvent(nSavings.BusinessEventType.AccountClosure, context);
                AddDatedSavingsAccountString(DatedSavingsAccountStringCode.SavingsAccountStatus.ToString(), SavingsAccountStatusCode.Closed.ToString(), context, savingsAccountNr: savingsAccountNr, businessEvent: evt);
                var h = context.SavingsAccountHeadersQueryable.Single(x => x.SavingsAccountNr == savingsAccountNr);
                h.Status = SavingsAccountStatusCode.Closed.ToString();
                context.SaveChanges();
            }            
        }
    }
}
