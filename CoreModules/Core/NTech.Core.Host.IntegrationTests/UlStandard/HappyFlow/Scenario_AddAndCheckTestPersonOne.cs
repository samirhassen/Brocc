using NTech.Core.Customer.Database;
using NTech.Core.Host.IntegrationTests.UlStandard;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlStandardLikeScenarioTests
    {
        private void AddAndCheckTestPersonOne(UlStandardTestRunner.TestSupport support)
        {
            var customerId = TestPersons.EnsureTestPerson(support, 1);

            using var context = new CustomerContext();

            // Ensure that the mapping between customer id and civicRegNr is preserved
            var customerIdSequence = context.CustomerIdSequences.Single(x => x.CustomerId == customerId);
            Assert.That(customerIdSequence.CivicRegNrHash, Is.EqualTo("OXpfYCGViIC5BHRjqayaH/NNTB/4MCFXq5ie5hn4IMvlQd4QSA0QcSYQlqX3ty5c"));

            var customerProperties = context.CustomerProperties.Where(x => x.CustomerId == customerId).ToList();

            // Birth date should be added by the infrastructure if it can be computed from civicRegNr
            Assert.That(
                customerProperties.Single(x => x.Name == "birthDate").Value,
                Is.EqualTo("1975-02-15"));

            // Address hash should be added to support address search
            Assert.That(
                customerProperties.Single(x => x.Name == "addressHash").Value,
                Is.EqualTo("d35f092fc9f306c1523509813ce62dca"));

            var actualSearchTerms = context
                .CustomerSearchTerms
                .Where(x => x.CustomerId == customerId)
                .ToDictionary(x => x.TermCode, x => x.Value);

            // Name phonetic search translation
            Assert.That(actualSearchTerms["firstName"], Is.EqualTo("ASKL"));
            Assert.That(actualSearchTerms["lastName"], Is.EqualTo("AKLN"));

            //Normalized phonenr
            Assert.That(actualSearchTerms["phone"], Is.EqualTo("+46243209897"));

            //Hashed email
            Assert.That(actualSearchTerms["email"], Is.EqualTo("VB9PEZD8zl4q0f7MYAx54w=="));
        }
    }
}