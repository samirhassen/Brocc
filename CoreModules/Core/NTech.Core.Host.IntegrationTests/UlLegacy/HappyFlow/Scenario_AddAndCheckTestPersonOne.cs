using NTech.Core.Customer.Database;
using NTech.Core.Host.IntegrationTests.UlLegacy;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlLegacyScenarioTests
    {
        private void AddAndCheckTestPersonOne(UlLegacyTestRunner.TestSupport support)
        {
            var customerId = TestPersons.EnsureTestPerson(support, 1);

            using var context = new CustomerContext();

            // Ensure that the mapping between customer id and civicRegNr is preserved
            var customerIdSequence = context.CustomerIdSequences.Single(x => x.CustomerId == customerId);
            Assert.That(customerIdSequence.CivicRegNrHash, Is.EqualTo("gD6WCKhxXT4ECv0OoX59JF5UInRMjvrtT6h51zIeO/XVlRG83afR28qqDrJgtQ5W"));

            var customerProperties = context.CustomerProperties.Where(x => x.CustomerId == customerId).ToList();

            // Birth date should be added by the infrastructure if it can be computed from civicRegNr
            Assert.That(
                customerProperties.Single(x => x.Name == "birthDate").Value,
                Is.EqualTo("1975-02-15"));

            // Address hash should be added to support address search
            Assert.That(
                customerProperties.Single(x => x.Name == "addressHash").Value,
                Is.EqualTo("ba5c0203dea30529351b59bf4f1d6e2e"));
            Assert.That(customerProperties.Single(x => x.Name == "addressZipcode").Value, Is.EqualTo("90530"));

            var actualSearchTerms = context
                .CustomerSearchTerms
                .Where(x => x.CustomerId == customerId)
                .ToDictionary(x => x.TermCode, x => x.Value);

            // Name phonetic search translation
            Assert.That(actualSearchTerms["firstName"], Is.EqualTo("ANTR"));
            Assert.That(actualSearchTerms["lastName"], Is.EqualTo("PRNL"));

            //Normalized phonenr
            Assert.That(actualSearchTerms["phone"], Is.EqualTo("+358243209897"));

            //Hashed email
            Assert.That(actualSearchTerms["email"], Is.EqualTo("zgZ4uHHPmlTYSdb+osyrcQ=="));
        }
    }
}