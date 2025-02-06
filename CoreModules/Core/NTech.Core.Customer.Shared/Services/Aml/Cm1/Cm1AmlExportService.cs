using NTech.Core.Customer.Shared.Database;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;

namespace nCustomer.Code.Services.Aml.Cm1
{
    public class Cm1AmlExportService
    {
        private readonly Func<ICustomerContextExtended, CustomerWriteRepository> createCustomerRepository;
        private readonly Lazy<NTechSimpleSettingsCore> cm1Settings;
        private readonly CustomerContextFactory contextFactory;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly IDocumentClient documentClient;
        private readonly INTechEnvironment environment;

        public Cm1AmlExportService(Func<ICustomerContextExtended, CustomerWriteRepository> createCustomerRepository, Lazy<NTechSimpleSettingsCore> cm1Settings,
            CustomerContextFactory contextFactory, IClientConfigurationCore clientConfiguration, IDocumentClient documentClient, 
            INTechEnvironment environment)
        {
            this.createCustomerRepository = createCustomerRepository;
            this.cm1Settings = cm1Settings;
            this.contextFactory = contextFactory;
            this.clientConfiguration = clientConfiguration;
            this.documentClient = documentClient;
            this.environment = environment;
        }

        public CmlExportFileResponse CreateCm1AmlExportFilesAndUpdateCustomerExportStatus(PerProductCmlExportFileRequest request)
        {
            var completeRequest = new CompleteCmlExportFileRequest
            {
                ProductRequest = request,
            };

            if (request.Savings)
                completeRequest.ExportType = CompleteCmlExportFileRequest.ExportTypeCode.Savings;
            if (request.Credits)
                completeRequest.ExportType = CompleteCmlExportFileRequest.ExportTypeCode.Credit; //NOTE: this hides savings = true, preserving the crazy for now

            var relationTypesSettingNameName = GetRelationTypesSettingNameByExportTypeCode(completeRequest.ExportType);

            var repo = new Cm1AmlDataRepository(contextFactory);
            completeRequest.Customers = repo.FetchCustomersToExport(cm1Settings.Value.Req(relationTypesSettingNameName).ToString());

            var exportFiles = CreateCm1AmlExportFiles(completeRequest);

            repo.UpdateSentToCm1(completeRequest.Customers, createCustomerRepository);

            return exportFiles;
        }

        private CmlExportFileResponse CreateCm1AmlExportFiles(CompleteCmlExportFileRequest request)
        {
            var fileFormat = new Cm1FileFormat(cm1Settings, contextFactory, clientConfiguration);
            var deliveryDate = DateTimeOffset.Now;

            var product = GetProductName(request.ExportType.Value);

            Func<string, string, string> storeFile = (typename, tempFileName) =>
            {
                var actualFileName = cm1Settings.Value.Req("FileSuffix") + "_" + typename + product + $"_{deliveryDate.ToString("yyyyMMddHHmmss")}.xml";
                return documentClient.ArchiveStoreFile(new FileInfo(tempFileName), "application/xml", actualFileName);
            };

            string deliveryArchiveKeyCustomers = null;
            var deliveryArchiveKeyTransactions = new List<string>();

            fileFormat.CreateExportFiles(request,
                tempFileName => deliveryArchiveKeyCustomers = storeFile("Customers", tempFileName),
                tempFileName => deliveryArchiveKeyTransactions.Add(storeFile("Transactions", tempFileName)),
                createCustomerRepository);

            return new CmlExportFileResponse
            {
                CustomerFileArchiveKey = deliveryArchiveKeyCustomers,
                TransactionFileArchiveKeys = deliveryArchiveKeyTransactions
            };
        }

        public static string GetProductName(CompleteCmlExportFileRequest.ExportTypeCode exportTypeCode)
        {
            switch (exportTypeCode)
            {
                case CompleteCmlExportFileRequest.ExportTypeCode.Savings: return "Savings";
                case CompleteCmlExportFileRequest.ExportTypeCode.Credit: return "Credit";
                default:
                    throw new NotImplementedException();
            }
        }

        public static string GetRelationTypesSettingNameByExportTypeCode(CompleteCmlExportFileRequest.ExportTypeCode? exportTypeCode)
        {
            switch (exportTypeCode ?? CompleteCmlExportFileRequest.ExportTypeCode.Error)
            {
                case CompleteCmlExportFileRequest.ExportTypeCode.Savings: return "SavingsRelationTypes";
                case CompleteCmlExportFileRequest.ExportTypeCode.Credit: return "CreditsRelationTypes";
                default:
                    throw new NTechCoreWebserviceException("Missing Savings or Credit")
                    {
                        ErrorHttpStatusCode = 400,
                        IsUserFacing = true
                    };
            }
        }

        public List<string> SendUpdatesForAllCustomersReturningErrors()
        {
            var sendAsUpdate = !environment.OptBoolSetting("ntech.cm1.SendUpdatesForAllCustomers.forceInsert");
            var errors = new List<string>();

            SendUpdatesForAllCustomers("ntech.feature.ullegacy", "Cm1AmlExportProfileNameCustomersCredits",
                CompleteCmlExportFileRequest.ExportTypeCode.Credit, sendAsUpdate, errors);

            SendUpdatesForAllCustomers("ntech.feature.savings", "Cm1AmlExportProfileNameCustomersSavings",
                CompleteCmlExportFileRequest.ExportTypeCode.Savings, sendAsUpdate, errors);

            return errors;
        }

        public static NTechSimpleSettingsCore GetCm1Settings(INTechEnvironment environment)
        {
            var file = environment.StaticResourceFile("ntech.credit.cm1.settingsfile", "cm1-business-credit-settings.txt", false);
            if (!file.Exists)
                throw new Exception("The configuration file ntech.credit.cm1.settingsfile is missing!");
            var f = NTechSimpleSettingsCore.ParseSimpleSettingsFile(file.FullName, forceFileExistance: true);
            return f;
        }

        private void SendUpdatesForAllCustomers(string productFeatureToggle, string customerFileExportProfileSettingName,
            CompleteCmlExportFileRequest.ExportTypeCode exportTypeCode, bool sendAsUpdate, List<string> errors)
        {
            if (!clientConfiguration.IsFeatureEnabled(productFeatureToggle))
                return;

            var relationTypeSettingName = GetRelationTypesSettingNameByExportTypeCode(exportTypeCode);

            var cm1AmlDataRepository = new Cm1AmlDataRepository(contextFactory);
            var fileFormat = new Cm1FileFormat(cm1Settings, contextFactory, clientConfiguration);            

            var productCustomers = cm1AmlDataRepository.FetchAllCustomersCurrentlySentToCm1(cm1Settings.Value.Req(relationTypeSettingName).ToString(), sendAsUpdate);
            fileFormat.CreateExportFiles(new CompleteCmlExportFileRequest
            {
                Customers = productCustomers,
                ExportType = exportTypeCode,
                ProductRequest = new PerProductCmlExportFileRequest
                {
                    Credits = exportTypeCode == CompleteCmlExportFileRequest.ExportTypeCode.Credit,
                    Savings = exportTypeCode == CompleteCmlExportFileRequest.ExportTypeCode.Savings,
                    Transactions = new System.Collections.Generic.List<PerProductCmlExportFileRequest.TransactionModel>()
                }
            }, customerFileName =>
            {
                var typename = "Customers";
                var product = GetProductName(exportTypeCode);
                var actualFileName = cm1Settings.Value.Req("FileSuffix") + "_" + typename + product + $"_{DateTimeOffset.Now.ToString("yyyyMMddHHmmss")}.xml";

                var exportProfileName = cm1Settings.Value.Req(customerFileExportProfileSettingName);

                var archiveKey = documentClient.ArchiveStoreFile(new FileInfo(customerFileName), "application/xml", actualFileName);
                var exportResult = documentClient.ExportArchiveFile(archiveKey, exportProfileName, null);
                if (!exportResult.IsSuccess)
                    errors.Add($"Failed to export {exportTypeCode} to {exportProfileName}");
                documentClient.DeleteArchiveFile(archiveKey);
            }, _ =>
            {
                throw new Exception("No transaction file expected");
            }, createCustomerRepository);
        }
    }
}