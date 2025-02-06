namespace NTech.Core.Host.IntegrationTests.UlLegacy.Savings
{
    public partial class SavingsFiHappyFlowTests
    {
        [Test]
        public void CreateSavingsAccount()
        {
            UlLegacyTestRunner.RunTestStartingFromEmptyDatabases(x =>
            {
                CreateSavingsAccountTest(x, isFrozen: false);
                AddDeposit(x);
                ExportAccountToCm1(x);
                ExportAccountToCustoms(x);
            });
        }

        [Test]
        public void CreateSavingsAccountFrozen()
        {
            UlLegacyTestRunner.RunTestStartingFromEmptyDatabases(x =>
            {
                CreateSavingsAccountTest(x, isFrozen: true);
            });
        }
    }    
}
