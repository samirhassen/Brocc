using nSavings.Code.Cm1;
using NTech.Core.Host.IntegrationTests.Shared.Services;
using NTech.Core.Host.IntegrationTests.UlLegacy.Utilities;
using NTech.Core.Module;
using NTech.Services.Infrastructure;
using NuGet.Frameworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NTech.Core.Host.IntegrationTests.UlLegacy.Savings
{
    public partial class SavingsFiHappyFlowTests
    {
        internal void ExportAccountToCm1(UlLegacyTestRunner.TestSupport support)
        {
            using var tempDir = new TemporaryDirectory();
            var cm1Settings = SharedCustomer.CreateCm1Settings(tempDir);
            var exportedFiles = new List<XDocument>();
            var customerClient = TestPersons.CreateRealisticCustomerClient(support, observeCm1ExportedFiles: x =>
            {
                exportedFiles.Add(x);
            });
            var model = SavingsCm1DomainModel.GetChangesSinceLastExport(support.CurrentUser.UserId, support.CurrentUser.InformationMetadata,
                support.CreateSavingsContextFactory(), new Lazy<NTechSimpleSettingsCore>(() => cm1Settings), customerClient.Object, support.EncryptionService);
            var cmlExportFileResponse = customerClient.Object.CreateCm1AmlExportFiles(new Module.Shared.Clients.PerProductCmlExportFileRequest
            {
                Transactions = model.Transactions,
                Savings = true
            });

            //Update timestamps when the files been delivered
            model.UpdateChangeTrackingSystemItems();

            try
            {
                var customerFile = new XmlDocumentHelper(exportedFiles.First());
                var transactionFile = new XmlDocumentHelper(exportedFiles.Skip(1).Single());
                Assert.Multiple(() =>
                {
                    Assert.That(customerFile.GetElementValue("Import", "Inserts", "Person", "FirstName"), Is.EqualTo("Fredrik"));
                    Assert.That(customerFile.GetElementValue("Import", "Inserts", "Person", "ContactInformation", "Country"), Is.EqualTo("FI"));

                    Assert.That(transactionFile.GetElementValue("Import", "Transactions", "Transaction[0]", "Amount"), Is.EqualTo("1000.00"));
                    Assert.That(transactionFile.GetElementValue("Import", "Transactions", "Transaction[1]", "Amount"), Is.EqualTo("100.00"));
                    Assert.That(transactionFile.GetElementValue("Import", "Transactions", "Transaction[1]", "TransactionAdditionalData", "ParameterValue"), Is.EqualTo("Test person"));                    
                });
            }
            catch
            {
                foreach(var exportedFile in exportedFiles)
                {
                    TestContext.WriteLine(exportedFile.ToString());
                }
                throw;
            }
        }        
    }
}
