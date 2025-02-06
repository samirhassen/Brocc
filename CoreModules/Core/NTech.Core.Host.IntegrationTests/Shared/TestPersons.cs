using Moq;
using NTech.Core.Customer.Database;
using NTech.Core.Customer.Services;
using NTech.Core.Host.IntegrationTests.Shared.Services;
using NTech.Core.Module.Shared.Clients;
using nTest.RandomDataSource;
using System.Xml.Linq;

namespace NTech.Core.Host.IntegrationTests
{
    internal static class TestPersons
    {
        const int InitialRandomPersonSeed = 100000;
        private static int nextRandomPersonSeed = InitialRandomPersonSeed;

        public static ISet<int> CreateRandomTestsPersons(SupportShared support, int count, params int[] personSeeds)
        {
            if (personSeeds.Length > 0)
            {
                if (personSeeds.Length != count)
                    throw new Exception("If seeds are provided they need to be the same number as the nr persons generated");
                if (personSeeds.Any(x => x >= InitialRandomPersonSeed))
                    throw new Exception($"Non random seeds need to be < {InitialRandomPersonSeed} to not clash with random ones");
            }

            using var context = new CustomerContext();

            context.ChangeTracker.AutoDetectChangesEnabled = false;

            var customerCreationService = new CustomerCreationService(support.Clock, support.CurrentUser, support.ClientConfiguration);

            var tp = TestPersonGenerator.GetSharedInstance(support.ClientConfiguration.Country.BaseCountry);
            var customerIds = new HashSet<int>(count);
            var i = 0;
            while (customerIds.Count < count)
            {
                var seed = personSeeds.Length > 0 ? personSeeds[i++] : nextRandomPersonSeed++;
                var seedKey = $"CustomerIdBySeed_{seed}";
                if (support.Context.ContainsKey(seedKey))
                {
                    customerIds.Add((int)support.Context[seedKey]);
                }
                else
                {
                    var random = new RandomnessSource(seed);
                    var civicNr = tp.GenerateCivicRegNumber(random);
                    var personData = tp.GenerateTestPerson(random, civicNr, true, support.Clock.Now.DateTime);
                    var createPersonRequestItems = new Dictionary<string, string>(personData);
                    createPersonRequestItems.Remove("civicRegNr");
                    var customerId = customerCreationService.CreateNewPerson(context, civicNr, createPersonRequestItems);
                    support.Context[$"TestPersonByCustomerId{customerId}_Data"] = personData;
                    support.Context[seedKey] = customerId;
                    customerIds.Add(customerId);
                }
            }

            context.SaveChanges();

            return customerIds;
        }

        /// <returns>customerId</returns>
        public static int EnsureTestPerson(SupportShared support, int personSeed) =>
            CreateRandomTestsPersons(support, 1, personSeed).Single();

        public static Mock<ICustomerClient> CreateRealisticCustomerClient(SupportShared support,
            Action<XDocument>? observeCm1ExportedFiles = null, bool forceMockBulkFetch = false) => SharedCustomer.CreateClient(
                support, observeCm1ExportedFiles: observeCm1ExportedFiles, forceMockBulkFetch: forceMockBulkFetch);        

        public static int GetTestPersonCustomerIdBySeed(SupportShared support, int seedNr)
        {
            return (int)support.Context[$"CustomerIdBySeed_{seedNr}"];
        }

        public static Dictionary<string, string> GetTestPersonDataBySeed(SupportShared support, int seedNr) =>
            GetTestPersonDataByCustomerId(support, GetTestPersonCustomerIdBySeed(support, seedNr));

        public static Dictionary<string, string> GetTestPersonDataByCustomerId(SupportShared support, int customerId) => 
            (Dictionary<string, string>)support.Context[$"TestPersonByCustomerId{customerId}_Data"];
        
    }
}
