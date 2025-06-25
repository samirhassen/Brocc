using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NTech.Banking.CivicRegNumbers.Fi;
using NTech.Banking.Shared.BankAccounts.Fi;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Core.Savings.Shared.Database;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;

namespace NTech.Core.Savings.Shared.Services.FinnishCustomsAccounts
{
    public class FinnishCustomsAccountsService
    {
        private readonly ICoreClock clock;
        private readonly Func<string, string> getUserDisplayNameByUserId;
        private readonly Func<string, string> getArchiveDocumentUrl;
        private readonly Lazy<NTechSimpleSettingsCore> finnishCustomsSettings;
        private readonly FinnishCustomsFileFormat fileFormat;
        private readonly ICustomerClient customerClient;
        private readonly IFinnishCustomsAccountsWebservice finnishCustomsAccountsWebservice;
        private readonly ILoggingService loggingService;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly IDocumentClient documentClient;
        private readonly IFinnishCustomsMigrationManager migrationManager;
        private readonly SavingsContextFactory contextFactory;
        public const string FileType = "FinnishCustomsAccounts";

        public FinnishCustomsAccountsService(ICoreClock clock, Func<string, string> getUserDisplayNameByUserId, Func<string, string> getArchiveDocumentUrl, 
            Lazy<NTechSimpleSettingsCore> finnishCustomsSettings, FinnishCustomsFileFormat fileFormat, ICustomerClient customerClient,
            IFinnishCustomsAccountsWebservice finnishCustomsAccountsWebservice, ILoggingService loggingService, IClientConfigurationCore clientConfiguration,
            IDocumentClient documentClient, IFinnishCustomsMigrationManager migrationManager, SavingsContextFactory contextFactory)
        {
            this.clock = clock;
            this.getUserDisplayNameByUserId = getUserDisplayNameByUserId;
            this.getArchiveDocumentUrl = getArchiveDocumentUrl;
            this.finnishCustomsSettings = finnishCustomsSettings;
            this.fileFormat = fileFormat;
            this.customerClient = customerClient;
            this.finnishCustomsAccountsWebservice = finnishCustomsAccountsWebservice;
            this.loggingService = loggingService;
            this.clientConfiguration = clientConfiguration;
            this.documentClient = documentClient;
            this.migrationManager = migrationManager;
            this.contextFactory = contextFactory;
        }

        private List<FinnishCustomsFileFormat.UpdateModel> CreateAccountsUpdateModels(int batchSize, ISavingsContext context)
        {
            var allAccounts = context
                .SavingsAccountHeadersQueryable
                .Select(x => new
                {
                    x.SavingsAccountNr,
                    x.MainCustomerId,
                    StartDate = x.CreatedByEvent.TransactionDate,
                    x.Status,
                    StatusDate = x
                        .DatedStrings
                        .Where(y => y.Name == DatedSavingsAccountStringCode.SavingsAccountStatus.ToString())
                        .OrderByDescending(y => y.BusinessEventId)
                        .Select(y => (DateTime?)y.TransactionDate)
                        .FirstOrDefault(),
                    WithdrawalIban = x
                        .DatedStrings
                        .Where(y => y.Name == DatedSavingsAccountStringCode.WithdrawalIban.ToString())
                        .OrderByDescending(y => y.BusinessEventId)
                        .Select(y => y.Value)
                        .FirstOrDefault(),
                })
                .ToArray();

            var changedCustomers = new List<FinnishCustomsFileFormat.Customer>();
            var changedAccounts = new List<FinnishCustomsFileFormat.Account>();
            var newCustomers = new List<FinnishCustomsFileFormat.Customer>();

            // Batch the diff-checks to avoid any SQL-issues when querying too many ids at once. 
            foreach (var currentAccountsBatch in allAccounts.SplitIntoGroupsOfN(250))
            {
                var customerDataForBatch = customerClient.BulkFetchPropertiesByCustomerIdsD(currentAccountsBatch.Select(x => x.MainCustomerId).ToHashSetShared(), "firstName", "lastName", "civicRegNr", "birthDate");

                var batchAccountNrs = new HashSet<string>(currentAccountsBatch.Select(ac => ac.SavingsAccountNr));

                var latestSentAccounts = KeyValueStoreService.GetValuesComposable(context, batchAccountNrs, KeyValueStoreKeySpaceCode.FinnishCustomsLatestExportAccountV1.ToString());
                var latestSentCustomers = KeyValueStoreService.GetValuesComposable(context, currentAccountsBatch.Select(x => x.MainCustomerId.ToString()).ToHashSetShared(), KeyValueStoreKeySpaceCode.FinnishCustomsLatestExportCustomersV1.ToString());

                foreach (var acc in currentAccountsBatch)
                {
                    var accountModel = new FinnishCustomsFileFormat.Account
                    {
                        AccountNr = acc.SavingsAccountNr,
                        WithdrawalIban = acc.WithdrawalIban == null ? null : IBANFi.Parse(acc.WithdrawalIban),
                        UseWithdrawalIbanAsId = false,
                        StartDate = acc.StartDate,
                        EndDate = acc.Status == SavingsAccountStatusCode.Closed.ToString() ? acc.StatusDate : null,
                        OwnerCustomerId = acc.MainCustomerId
                    };
                    var accountJson = JsonConvert.SerializeObject(accountModel);

                    if (latestSentAccounts.Opt(acc.SavingsAccountNr) != accountJson)
                    {
                        changedAccounts.Add(accountModel);
                    }
                }

                // Calculate which customers to send in. 
                foreach (var entry in customerDataForBatch)
                {
                    var customerId = entry.Key;
                    var data = entry.Value;

                    var civicRegNr = CivicRegNumberFi.Parse(data.Opt("civicRegNr"));
                    var birthDate = DateTimeUtilities.ParseExact(data.Opt("birthDate"), "yyyy-MM-dd") ?? civicRegNr.BirthDate;

                    var customerObject = new FinnishCustomsFileFormat.Customer
                    {
                        CustomerId = customerId,
                        CivicRegNr = civicRegNr,
                        BirthDate = birthDate,
                        FirstName = data.Opt("firstName"),
                        LastName = data.Opt("lastName")
                    };

                    var customerJson = JsonConvert.SerializeObject(customerObject);
                    if (latestSentCustomers.Opt(customerId.ToString()) == null)
                    {
                        if (!newCustomers.Any(x => x.CustomerId == customerId))
                            newCustomers.Add(customerObject);
                    }
                    else if (latestSentCustomers.Opt(customerId.ToString()) != customerJson
                        && !changedCustomers.Any(x => x.CustomerId == customerId))
                    {
                        changedCustomers.Add(customerObject);
                    }
                }
            }

            var updateModels = new List<FinnishCustomsFileFormat.UpdateModel>();
            foreach (var changedAccountsBatch in changedAccounts.ToArray().SplitIntoGroupsOfN(batchSize))
            {
                var accounts = changedAccountsBatch.ToList();

                var m = new FinnishCustomsFileFormat.UpdateModel
                {
                    SystemClientName = clientConfiguration.ClientName,
                    SenderBusinessId = finnishCustomsSettings.Value.Req("senderBusinessId"),
                    CreationDate = DateTime.Now, //NOTE: Intentionally doesn't use the time machine since the receiver expects calendar time
                    SavingsAccounts = new List<FinnishCustomsFileFormat.Account>(),
                    Customers = new List<FinnishCustomsFileFormat.Customer>()
                };

                foreach (var a in accounts)
                {
                    m.SavingsAccounts.Add(a);
                    // If the customers has not been saved before, i.e. new savingsaccount with new customer
                    // Then we must add it in the same file since the referenced legalPerson in Tulli must exist. 
                    if (newCustomers.Any(c => c.CustomerId == a.OwnerCustomerId))
                    {
                        var customerObj = newCustomers.Single(x => x.CustomerId == a.OwnerCustomerId);
                        // Ensure the same customer is not added more than once per file (two accounts with same owner)
                        if (!m.Customers.Any(x => x.CustomerId == a.OwnerCustomerId))
                        {
                            m.Customers.Add(customerObj);
                        }
                    }
                }

                updateModels.Add(m);
            }

            // Splits accounts and customers/legalPersons to separate files. 
            foreach (var customersBatch in changedCustomers.ToArray().SplitIntoGroupsOfN(batchSize))
            {
                var customers = customersBatch.ToList();

                var m = new FinnishCustomsFileFormat.UpdateModel
                {
                    SystemClientName = clientConfiguration.ClientName,
                    SenderBusinessId = finnishCustomsSettings.Value.Req("senderBusinessId"),
                    CreationDate = DateTime.Now, //NOTE: Intentionally doesn't use the time machine since the receiver expects calendar time
                    SavingsAccounts = new List<FinnishCustomsFileFormat.Account>(),
                    Customers = new List<FinnishCustomsFileFormat.Customer>()
                };

                foreach (var customer in customers)
                {
                    m.Customers.Add(customer);
                }

                updateModels.Add(m);
            }

            return updateModels;
        }

        public void CreateAndDeliverUpdate(INTechCurrentUserMetadata ntechCurrentUser,
            Action<string> observeArchiveKey = null,
            bool? skipArchive = null, bool? skipDeliver = null,
            Action<string> observeError = null)
        {
            //NOTE: So customs ninja added a ridiculously low size limit where the entire JWT can only be 50k. It's 1k with one customer with short name so even 25 might be to small. Leaving it configurable.
            var batchSize = int.Parse(finnishCustomsSettings.Value.Opt("delivery.batchSize") ?? "25");

            using (var context = contextFactory.CreateContext())
            {
                var models = CreateAccountsUpdateModels(batchSize, context);

                if (skipArchive.GetValueOrDefault())
                    return;

                var (filesToZip, modelAndTulliFiles) = GetFilesAndRawData(models, fileFormat);
                // Save here, after we are sure that the 
                var zipFile = migrationManager.CreateFlatZipFile(filesToZip.ToArray());

                var zipArchiveKey = documentClient.ArchiveStore(zipFile.ToArray(), "application/zip", $"FinnishCustomsExportPackage-{clock.Now:yyyyMMddHHmmss}.zip");
                observeArchiveKey?.Invoke(zipArchiveKey);

                var h = new OutgoingExportFileHeader
                {
                    TransactionDate = clock.Today,
                    FileArchiveKey = zipArchiveKey,
                    ChangedById = ntechCurrentUser.UserId,
                    InformationMetaData = ntechCurrentUser.InformationMetadata,
                    ChangedDate = clock.Now,
                    FileType = FileType,
                    CustomData = null,
                    ExportResultStatus = FinnishCustomsAccountsExportResultStatusCode.Created.ToString()
                };
                context.AddOutgoingExportFileHeaders(h);

                context.SaveChanges();

                if (skipDeliver.GetValueOrDefault() || finnishCustomsSettings.Value.OptBool("delivery.skip"))
                {
                    h.ExportResultStatus = FinnishCustomsAccountsExportResultStatusCode.DeliverySkipped.ToString();
                    context.SaveChanges();
                }
                else
                {
                    foreach (var (model, tulliFile) in modelAndTulliFiles)
                    {
                        var wsContext = new FinnishCustomsAccountsWebservice.LoggingContextModel();
                        try
                        {
                            if (!finnishCustomsAccountsWebservice.TryReportUpdate(   tulliFile, wsContext))
                            {
                                throw new Exception($"Finnish customs export failed with error or validation error: {wsContext.CustomsErrorMessage}");
                            }
                            SaveSentJsonToKeyValueStore(context, model, ntechCurrentUser);
                            context.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            loggingService.Error(ex, $"Finnish customs export failed {wsContext.CustomsCorrelationId}");
                            observeError?.Invoke("Export failed");
                            h.ExportResultStatus = FinnishCustomsAccountsExportResultStatusCode.DeliveryFailed.ToString();
                            wsContext.ExceptionMessage = ex.FormatException();
                            h.CustomData = JsonConvert.SerializeObject(wsContext);
                            context.SaveChanges();
                            return;
                        }
                    }
                    h.ExportResultStatus = FinnishCustomsAccountsExportResultStatusCode.Delivered.ToString();
                    h.CustomData = JsonConvert.SerializeObject(new { });
                    context.SaveChanges();
                }
            }
        }

        private void SaveSentJsonToKeyValueStore(ISavingsContext context, FinnishCustomsFileFormat.UpdateModel model, INTechCurrentUserMetadata currentUser)
        {
            foreach (var account in model.SavingsAccounts)
            {
                var json = JsonConvert.SerializeObject(account);

                KeyValueStoreService.SetValueComposable(context, account.AccountNr, KeyValueStoreKeySpaceCode.FinnishCustomsLatestExportAccountV1.ToString(), json,
                    currentUser.UserId, currentUser.InformationMetadata, clock);
            }

            foreach (var customer in model.Customers)
            {
                var json = JsonConvert.SerializeObject(customer);

                KeyValueStoreService.SetValueComposable(context, customer.CustomerId.ToString(), KeyValueStoreKeySpaceCode.FinnishCustomsLatestExportCustomersV1.ToString(), json, currentUser.UserId,
                    currentUser.InformationMetadata, clock);
            }
        }

        private (List<Tuple<string, Stream>> filesToZip, List<(FinnishCustomsFileFormat.UpdateModel model, JObject tulliFile)> rawData) GetFilesAndRawData(List<FinnishCustomsFileFormat.UpdateModel> models,
            FinnishCustomsFileFormat fileFormat)
        {
            var modelAndCorrespondingTulliFiles = new List<(FinnishCustomsFileFormat.UpdateModel, JObject)>();
            var filesForZip = new List<Tuple<string, Stream>>();

            // Create raw data for Tulli-requests and json model to save in our zip. 
            foreach (var model in models.Select((data, index) => (data, index)))
            {
                migrationManager.ValidateAndThrowOnError(model.data);

                var rawFile = fileFormat.CreateUpdateFileRaw(model.data);
                modelAndCorrespondingTulliFiles.Add((model.data, rawFile));
                filesForZip.Add(Tuple.Create($"tulliDataModel-{model.index}.json", (Stream)new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(rawFile)))));
                filesForZip.Add(Tuple.Create($"internalDataModel-{model.index}.json", (Stream)new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model.data)))));
            }

            return (filesForZip, modelAndCorrespondingTulliFiles);
        }

        public enum FinnishCustomsAccountsExportResultStatusCode
        {
            Created,
            Delivered,
            DeliveryFailed,
            DeliverySkipped
        }

        public Tuple<List<FinnishCustomsAccountsExportModel>, int> GetExportsPageAndNrOfPages(int pageSize, int pageNr)
        {
            using (var context = contextFactory.CreateContext())
            {
                var pre = context
                    .OutgoingExportFileHeadersQueryable
                    .Where(x => x.FileType == FileType)
                    .OrderByDescending(x => x.Id)
                    .AsQueryable();

                var totalCount = pre.Count();

                var nrOfPages = (totalCount / pageSize) + (totalCount % pageSize == 0 ? 0 : 1);

                pre = pre.Skip(pageSize * pageNr).Take(pageSize);

                var result = pre.Select(x => new
                {
                    x.Id,
                    x.FileArchiveKey,
                    x.ChangedById,
                    x.TransactionDate,
                    x.ExportResultStatus,
                    x.CustomData
                })
                .ToList()
                .Select(x =>
                {
                    return new FinnishCustomsAccountsExportModel
                    {
                        Id = x.Id,
                        TransactionDate = x.TransactionDate,
                        ArchiveKey = x.FileArchiveKey,
                        ExportResultStatus = x.ExportResultStatus,
                        UserId = x.ChangedById,
                        UserDisplayName = getUserDisplayNameByUserId(x.ChangedById.ToString()),
                        ArchiveDocumentUrl = x.FileArchiveKey == null ? null : getArchiveDocumentUrl(x.FileArchiveKey),
                        CustomData = x.CustomData
                    };
                })
                .ToList();

                return Tuple.Create(result, nrOfPages);
            }
        }
    }

    public class FinnishCustomsAccountsExportModel
    {
        public int Id { get; set; }
        public DateTime TransactionDate { get; set; }
        public string ArchiveKey { get; set; }
        public string ArchiveDocumentUrl { get; set; }
        public int UserId { get; set; }
        public string UserDisplayName { get; set; }
        public string ExportResultStatus { get; set; }
        public string CustomData { get; set; }
    }
}