using Moq;
using nCustomer;
using NTech.Core.Credit.Shared.Services.Aml.Cm1;
using NTech.Core.Customer.Database;
using NTech.Core.Customer.Shared.Services.Aml.Cm1;
using NTech.Core.Host.IntegrationTests.Shared.Services;
using NTech.Core.Host.IntegrationTests.UlLegacy;
using NTech.Core.Host.IntegrationTests.UlLegacy.Utilities;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Services.Infrastructure;
using System.Xml.Linq;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlLegacyScenarioTests
    {
        /*
         Trying to sort of reproduce what nCredit.ApiCm1AmlExportController does.
         This whole process should be remade so it does not jump back and forth between modules like this.
         */
        private void ExportToCm1(UlLegacyTestRunner.TestSupport support)
        {
            Cm1CustomerKycQuestionsRepository.DisableErrorSupression = true;
            CreditCm1AmlExportService.DisableErrorSupression = true;

            var exportedFiles = new List<XDocument>();
            var customerClient = TestPersons.CreateRealisticCustomerClient(support, observeCm1ExportedFiles: x => exportedFiles.Add(x));
            var documentClient = new Mock<IDocumentClient>(MockBehavior.Strict);
            var cm1Settings = new Lazy<NTechSimpleSettingsCore>(() =>
            {
                using var tempDir = new TemporaryDirectory();
                return SharedCustomer.CreateCm1Settings(tempDir);
            });

            var exportService = new CreditCm1AmlExportService(support.CreateCreditContextFactory(), support.EncryptionService, support.CurrentUser,
                customerClient.Object, cm1Settings, support.LoggingService, documentClient.Object);

            using(var context = new CustomerContext())
            {
                context.BeginTransaction();
                var customerId = TestPersons.GetTestPersonCustomerIdBySeed(support, 1);
                SharedCustomer.CreateCustomerRepository(context, support).UpdateProperties(new List<CustomerPropertyModel> 
                { 
                    new CustomerPropertyModel
                    {
                        CustomerId = customerId,
                        Name = "addressCountry",
                        Value = "SE"
                    }
                }, true);
                context.SaveChanges();
                context.CommitTransaction();
            }

            exportService.CreateExport(true);
            
            Assert.That(exportedFiles.Count, Is.EqualTo(2));
            var customerFile = new XmlDocumentHelper(exportedFiles[0]);
            var transactionFile = new XmlDocumentHelper(exportedFiles[1]);

            void AssertCustomerElement(string expected, params XmlDocumentHelper.ElementSelector[] actualPath) =>
                Assert.That(customerFile.GetElementValue(actualPath), Is.EqualTo(expected));

            void AssertTransactionElement(string expected, params XmlDocumentHelper.ElementSelector[] actualPath) =>
                Assert.That(transactionFile.GetElementValue(actualPath), Is.EqualTo(expected));

            try
            {
                Assert.Multiple(() =>
                {
                    AssertCustomerElement("true", "Import", "Settings", "InsertFallbackToUpdateWhenExisting");
                    AssertCustomerElement("90530", "Import", "Inserts", "Person", "ContactInformation", "PostalNumber");
                    AssertCustomerElement("SE", "Import", "Inserts", "Person", "ContactInformation", "Country");
                    AssertCustomerElement("Konsument kredit", "Import", "Inserts", "Person", "PersonAdditionalData", "CustomerAdditionalDataElement", "ParameterValue");

                    AssertTransactionElement("50.00", "Import", "Transactions", "Transaction[0]", "Amount");
                    AssertTransactionElement("100.00", "Import", "Transactions", "Transaction[1]", "Amount");
                    AssertTransactionElement("Inbetalare Namn", "Import", "Transactions", "Transaction[1]", "TransactionAdditionalData", "ParameterName");
                    AssertTransactionElement("Pay Er", "Import", "Transactions", "Transaction[1]", "TransactionAdditionalData", "ParameterValue");
                });
            }
            catch
            {
                foreach (var file in exportedFiles)
                {
                    TestContext.WriteLine(file.ToString());
                }
                throw;
            }
        }
    }
}
