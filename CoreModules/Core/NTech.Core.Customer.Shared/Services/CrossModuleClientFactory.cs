using NTech.Core.Module.Shared.Clients;
using System;

namespace NTech.Core.Customer.Shared.Services
{
    public class CrossModuleClientFactory
    {
        private readonly Lazy<ICreditClient> creditClient;
        private readonly Lazy<ISavingsClient> savingsClient;
        private readonly Lazy<IPreCreditClient> preCreditClient;

        public CrossModuleClientFactory(Lazy<ICreditClient> creditClient, Lazy<ISavingsClient> savingsClient, Lazy<IPreCreditClient> preCreditClient)
        {
            this.creditClient = creditClient;
            this.savingsClient = savingsClient;
            this.preCreditClient = preCreditClient;
        }

        public ICreditClient CreditClient => creditClient.Value;
        public ISavingsClient SavingsClient => savingsClient.Value;
        public IPreCreditClient PreCreditClient => preCreditClient.Value;
    }
}
