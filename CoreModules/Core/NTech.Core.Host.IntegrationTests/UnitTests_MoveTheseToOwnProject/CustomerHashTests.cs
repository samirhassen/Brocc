using NTech.Core.Customer.Shared.Services;

namespace NTech.Core.Host.IntegrationTests.UnitTests_MoveTheseToOwnProject
{
    public class CustomerHashTests
    {
        [Test]
        [TestCase("270341-527D", "WfGya8sqpFPSN3jLrvPdGcA2KHfZW6jor2o447ZrxvKOt7NkMge2pS/u5RSn4F2V")]
        [TestCase("020492-845H", "M4s7ZHQSv8LA57/Ift23Lto1/GC5QTfKHLdiBzJ7fI+qFJJLKt1PyrIE3hm/ee1m")]
        [TestCase("260197-518A", "fftiwNl7saEqzwltPUBpWWZuuNLT/W+X3TVBRVz7/bAIPZRBEe55jOQz9lrRP1eY")]
        [TestCase("197902100424", "bcqiDWiASXRckBTySzWF2oUNMhg4A/9mrpK0BYO+wOEP9Hic8TaSdC90+2UJhFIh")]
        [TestCase("197211249649", "Sd2p1+W9c8LdUcKPFMQmt2GIAMUsR1hJuZqB4nxSPwBh2T0MwiLOxfI79ba6DmUa")]
        [TestCase("197604206628", "Td+oA/znpJn1tLmGipLcfUXluZsR/m6XmyYntAcbLoboIEwmTxrY64UMeYreEuz2")]
        [TestCase("6153341059", "92C3lzbFaUvvmB4BLUiqsIiTkgzb1zra0CBKCf1y0BCbVf4n7aUaZ7y9t2eLUtLa")]
        [TestCase("5590406483", "ZRemrhwbO0/wl6id0BKmBpdJZ+n8Zp3/9fvjmR5OS7GXQMgRldFKkw2UeG+eX6G4")]
        [TestCase("2021004151", "lahx9GmRpzbXW11pK3Sn74wIZfiBwW9oysUZ9NJfp+BE/OxpKID8DJqkjM8nd030")]
        [Parallelizable(ParallelScope.All)]
        public void EnsureLegacyAndCoreCustomerIdHashesAreTheSame(string civicRegNr, string expectedHash)
        {
            Assert.That(CustomerServiceBase.ComputeCustomerCivicOrOrgnrToCustomerIdMappingHash(civicRegNr), Is.EqualTo(expectedHash));

        }

        [Test]
        public void EnsureLegacyAndCoreAddressHashesAreTheSame()
        {
            var hash = CustomerServiceBase.ComputeAddressHash(new List<nCustomer.CustomerPropertyModel>
            {
                new nCustomer.CustomerPropertyModel { Name = "addressStreet", Value = "Inkereentie 91802" },
                new nCustomer.CustomerPropertyModel { Name = "addressZipcode", Value = "38700" },
                new nCustomer.CustomerPropertyModel { Name = "addressCity", Value = "LAHTI" }
            });
            Assert.That(hash, Is.EqualTo("6163ac57bdb485ac9b0a5b5a4a9a8f36"));
        }
    }
}
