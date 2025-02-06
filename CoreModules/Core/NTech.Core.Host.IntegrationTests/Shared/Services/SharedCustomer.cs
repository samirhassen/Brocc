using Microsoft.Extensions.DependencyInjection;
using Moq;
using nCustomer.Code.Services.Aml.Cm1;
using nCustomer.Code.Services.Kyc;
using nCustomer.Code.Services;
using nCustomer;
using Newtonsoft.Json;
using NTech.Banking.CivicRegNumbers;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Customer.Shared;
using NTech.Core.Customer.Shared.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Services.Infrastructure;
using System.Xml.Linq;
using NTech.Core.Customer.Database;
using KycCustomerOnboardingStatusModel_Client = NTech.Core.Module.Shared.Clients.KycCustomerOnboardingStatusModel;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Services;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.ElectronicSignatures;
using nCustomer.Services.EidSignatures;

namespace NTech.Core.Host.IntegrationTests.Shared.Services
{
    public static class SharedCustomer
    {
        public static void RegisterServices(SupportShared support, ServiceCollection services, Func<ServiceProvider> getProvider)
        {
            services.AddTransient<IUrlService>(_ => new Mock<IUrlService>(MockBehavior.Strict).Object);
            services.AddTransient(_ => support.CreateCustomerContextFactory());
            services.AddTransient(_ => support.CustomerEnvSettings);
            services.AddTransient<ICustomerClient>(x => CreateClient(support).Object);
            services.AddTransient(x => new CrossModuleClientFactory(
                new Lazy<ICreditClient>(() => new Mock<ICreditClient>(MockBehavior.Strict).Object),
                new Lazy<ISavingsClient>(() => new Mock<ISavingsClient>(MockBehavior.Strict).Object),
                new Lazy<IPreCreditClient>(() => x.GetRequiredService<IPreCreditClient>())));
            services.AddTransient<KycQuestionsSessionService>();
            services.AddTransient<KycQuestionsTemplateService>();
            services.AddTransient<KycAnswersUpdateService>();
            services.AddTransient<KycScreeningService>(x => CreateKycScreeningService(x, false, false));
            services.AddTransient<IKycScreeningService>(x => x.GetRequiredService<KycScreeningService>());
            services.AddTransient<KycManagementService>();
            services.AddTransient<IKycManagementService>(x => x.GetRequiredService<KycManagementService>());
        }

        public static Mock<ICustomerClient> CreateClient(SupportShared support,
            Action<XDocument>? observeCm1ExportedFiles = null, bool forceMockBulkFetch = false)
        {
            var customerClient = new Mock<ICustomerClient>(MockBehavior.Strict);
            
            customerClient
                .Setup(x => x.BulkFetchPropertiesByCustomerIdsD(It.IsAny<ISet<int>>(), It.IsAny<string[]>()))
                .Returns<ISet<int>, string[]>((customerIds, names) =>
                {
                    if (!forceMockBulkFetch)
                    {
                        using (var context = support.CreateCustomerContextFactory().CreateContext())
                        {
                            var repo = new CustomerSearchRepository(context, support.EncryptionService, support.ClientConfiguration);
                            return repo.BulkFetchD(customerIds, propertyNames: names?.ToHashSetShared());
                        }
                    }
                    else
                    {
                        var result = new Dictionary<int, Dictionary<string, string>>();
                        foreach (var customerId in customerIds)
                        {
                            var contextkey = $"TestPersonByCustomerId{customerId}_Data";
                            if (support.Context.ContainsKey(contextkey))
                                result[customerId] = (Dictionary<string, string>)support.Context[contextkey];

                        }
                        return result;
                    }
                });

            var customerContextFactory = new CustomerContextFactory(() => new CustomerContextExtended(support.CurrentUser, support.Clock));

            //Simulate the actual api call by calling the service directly on the customer side
            customerClient
                .Setup(x => x.AddCustomerQuestionsSet(It.IsAny<CustomerQuestionsSet>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns<CustomerQuestionsSet, string, string>((questions, sourceType, sourceId) =>
                {
                    var kycService = CreateKycManagementService(support);

                    var customerQuestions = JsonConvert.DeserializeObject<CustomerQuestionsSet>(JsonConvert.SerializeObject(questions));

                    return kycService.AddCustomerQuestionsSet(customerQuestions, sourceType, sourceId);
                });

            customerClient
                .Setup(x => x.UpdateCustomerCard(It.IsAny<List<CustomerClientCustomerPropertyModel>>(), It.IsAny<bool>()))
                .Callback<List<CustomerClientCustomerPropertyModel>, bool>((properties, force) =>
                {
                    using (var context = customerContextFactory.CreateContext())
                    {
                        context.BeginTransaction();
                        try
                        {
                            var repo = CreateCustomerRepository(context, support);
                            var localProperties = JsonConvert.DeserializeObject<List<CustomerPropertyModel>>(JsonConvert.SerializeObject(properties));
                            repo.UpdateProperties(localProperties, force);
                            context.SaveChanges();
                            context.CommitTransaction();
                        }
                        catch
                        {
                            context.RollbackTransaction();
                            throw;
                        }
                    }
                });

            customerClient
                .Setup(x => x.FetchCustomerOnboardingStatuses(It.IsAny<ISet<int>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns<ISet<int>, string, string, bool>((customerIds, sourceType, sourceId, includeLatestQuestionSets) =>
                {
                    var kycService = CreateKycManagementService(support);
                    var result = kycService.FetchKycCustomerOnboardingStatuses(customerIds, string.IsNullOrWhiteSpace(sourceType)
                        ? (sourceType, sourceId) : null, true);

                    return JsonConvert.DeserializeObject<Dictionary<int, KycCustomerOnboardingStatusModel_Client>>(JsonConvert.SerializeObject(result)!)!;
                });

            customerClient
                .Setup(x => x.CheckPropertyStatus(It.IsAny<int>(), It.IsAny<HashSet<string>>()))
                .Returns<int, HashSet<string>>((customerId, properties) =>
                {
                    var service = new CustomerPropertyStatusService(new CustomerContextFactory(() => new CustomerContextExtended(support.CurrentUser, support.Clock)));
                    if (!service.TryCheckPropertyStatus(customerId, properties.ToList(), out var failedMessage, out var result))
                        throw new Exception(failedMessage);
                    var data = JsonConvert.DeserializeObject<NTech.Core.Module.Shared.Clients.CustomerCardPropertyStatusResult>(JsonConvert.SerializeObject(result)!)!;
                    return data;
                });

            customerClient
                .Setup(x => x.MergeCustomerRelations(It.IsAny<List<CustomerClientCustomerRelation>>()))
                .Callback<List<CustomerClientCustomerRelation>>(relations =>
                {
                    var mergeService = new MergeCustomerRelationsService(new Lazy<string>(() => Module.NEnv.SharedInstance.GetConnectionString("CustomersContext")));
                    mergeService.MergeCustomerRelation(new MergeCustomerRelationsRequest
                    {
                        Relations = relations
                    });
                });

            customerClient
                .Setup(x => x.SendSecureMessage(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
                .Returns((int customerId, string channelId, string channelType, string text, bool notifyCustomerByEmail, string textFormat) =>
                {
                    return 42;
                });

            customerClient.Setup(x => x.LoadSettings("generalTermsHtmlTemplate")).Returns(new Dictionary<string, string> { ["generalTermsHtmlTemplate"] = "" });

            customerClient
                .Setup(x => x.GetCustomerId(It.IsAny<ICivicRegNumber>()))
                .Returns<ICivicRegNumber>(x =>
                {
                    using (var context = customerContextFactory.CreateContext())
                    {
                        return CustomerIdSourceCore.GetCustomerIdByCivicRegNr(x, context);
                    }
                });

            //TODO: Move over the checkpoint repo to core and call that
            customerClient
                .Setup(x => x.GetActiveCheckpointIdsOnCustomerIds(It.IsAny<HashSet<int>>(), It.IsAny<List<string>>()))
                .Returns(new Module.Shared.Clients.GetActiveCheckPointIdsOnCustomerIdsResult
                {
                    CheckPointByCustomerId = new Dictionary<int, Module.Shared.Clients.GetActiveCheckPointIdsOnCustomerIdsResult.CheckPoint>()
                });

            customerClient
                .Setup(x => x.CreateOrUpdatePerson(It.IsAny<CreateOrUpdatePersonRequest>()))
                .Returns<CreateOrUpdatePersonRequest>(request =>
                {
                    var personService = new PersonCustomerService(customerContextFactory, support.EncryptionService, support.ClientConfiguration);
                    ICivicRegNumber civicRegNumber = new CivicRegNumberParser(support.ClientConfiguration.Country.BaseCountry).Parse(request.CivicRegNr);
                    return personService.CreateOrUpdatePerson(civicRegNumber,
                        request?.Properties?.ToDictionary(x => x.Name, x => x.Value),
                        additionalSensitiveProperties: request?.AdditionalSensitiveProperties?.ToHashSetShared(),
                        expectedCustomerId: request?.ExpectedCustomerId,
                        externalEventCode: request?.EventSourceId != null && request?.EventType != null ? request?.EventType + "_" + request?.EventSourceId : null,
                        forceUpdateProperties: request?.Properties?.Where(x => x.ForceUpdate)?.Select(x => x.Name)?.ToHashSet() ?? new HashSet<string>());
                });

            customerClient
                .Setup(x => x.SetupCustomerKycDefaults(It.IsAny<SetupCustomerKycDefaultsRequest>()))
                .Returns<SetupCustomerKycDefaultsRequest>(request =>
                {
                    IUrlService urlService = new Mock<IUrlService>(MockBehavior.Strict).Object;
                    ICustomerEnvSettings envSettings = new Mock<ICustomerEnvSettings>(MockBehavior.Strict).Object;
                    var templateService = new KycQuestionsTemplateService(customerContextFactory, envSettings, support.ClientConfiguration);
                    var kycAnswerService = new KycAnswersUpdateService(customerContextFactory, support.CurrentUser, support.Clock, templateService, support.CreateCachedSettingsService(), support.EncryptionService);
                    var kycManagementService = new KycManagementService(customerContextFactory,
                        x => new CustomerWriteRepository(x, support.CurrentUser, support.Clock, support.EncryptionService, support.ClientConfiguration), urlService, support.ClientConfiguration, kycAnswerService);
                    var service = new CustomerKycDefaultsService(kycManagementService,
                        new CustomerPropertyStatusService(customerContextFactory),
                        support.ClientConfiguration, customerContextFactory,
                        (x, y) => new CustomerWriteRepository(x, y, support.Clock, support.EncryptionService, support.ClientConfiguration));
                    return service.SetupCustomerKycDefaults(request);
                });

            customerClient
                .Setup(x => x.GetCustomerIdsWithSameData(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((name, value) =>
                {
                    return new List<int>();
                });

            customerClient
                .Setup(x => x.FindCustomerIdsMatchingAllSearchTerms(It.IsAny<List<CustomerSearchTermModel>>()))
                .Returns<List<CustomerSearchTermModel>>(terms =>
                {
                    using (var context = customerContextFactory.CreateContext())
                    {
                        var repo = new CustomerSearchRepository(context, support.EncryptionService, support.ClientConfiguration);
                        return repo.FindCustomersMatchingAllSearchTerms(terms?.Select(x => Tuple.Create(x.TermCode, x.TermValue))?.ToArray());
                    }
                });

            customerClient
                .Setup(x => x.CreateCm1AmlExportFiles(It.IsAny<PerProductCmlExportFileRequest>()))
                .Returns<PerProductCmlExportFileRequest>(request =>
                {
                    var documentClient = new Mock<IDocumentClient>(MockBehavior.Strict);
                    documentClient
                        .Setup(x => x.ArchiveStoreFile(It.IsAny<FileInfo>(), "application/xml", It.IsAny<string>()))
                        .Returns<FileInfo, string, string>((file, _, __) =>
                        {
                            if (observeCm1ExportedFiles != null)
                                observeCm1ExportedFiles(XDocument.Load(file.FullName));
                            return Guid.NewGuid().ToString();
                        });
                    var env = new Mock<INTechEnvironment>();
                    using var tempDir = new TemporaryDirectory();
                    var cm1Service = new Cm1AmlExportService(
                        x => new nCustomer.CustomerWriteRepository(x, support.CurrentUser, support.Clock, support.EncryptionService, support.ClientConfiguration),
                        new Lazy<Module.NTechSimpleSettingsCore>(() => CreateCm1Settings(tempDir)), support.CreateCustomerContextFactory(),
                        support.ClientConfiguration, documentClient.Object, env.Object);
                    var result = cm1Service.CreateCm1AmlExportFilesAndUpdateCustomerExportStatus(new PerProductCmlExportFileRequest
                    {
                        Credits = request?.Credits ?? false,
                        Savings = request?.Savings ?? false,
                        Transactions = request?.Transactions?.Select(x => new PerProductCmlExportFileRequest.TransactionModel
                        {
                            Amount = x.Amount,
                            CustomerId = x.CustomerId,
                            Id = x.Id,
                            IsConnectedToIncomingPayment = x.IsConnectedToIncomingPayment,
                            IsConnectedToOutgoingPayment = x.IsConnectedToOutgoingPayment,
                            TransactionDate = x.TransactionDate,
                            TransactionCustomerName = x.TransactionCustomerName
                        })?.ToList()
                    });
                    return new Module.Shared.Clients.CmlExportFileResponse
                    {
                        CustomerFileArchiveKey = result.CustomerFileArchiveKey,
                        TransactionFileArchiveKeys = result.TransactionFileArchiveKeys
                    };
                });

            customerClient
                .Setup(x => x.CreateKycQuestionSession(It.IsAny<CreateKycQuestionSessionRequest>()))
                .Returns<CreateKycQuestionSessionRequest>(request =>
                {
                    var service = support.Services.GetRequiredService<KycQuestionsSessionService>();
                    return service.CreateSession(request);
                });

            customerClient
                .Setup(x => x.FetchKycQuestionSession(It.IsAny<string>()))
                .Returns<string>(sessionId =>
                {
                    var service = support.Services.GetRequiredService<KycQuestionsSessionService>();
                    return service.GetSession(sessionId);
                });

            customerClient
                .Setup(x => x.KycScreenNew(It.IsAny<ISet<int>>(), It.IsAny<DateTime>(), It.IsAny<bool>()))
                .Returns<ISet<int>, DateTime, bool>((customerIds, date, isNonBatchScreen) =>
                {
                    var service = support.Services.GetRequiredService<KycScreeningService>();
                    var result = service.ListScreenBatchNew(customerIds?.ToList(), date, isNonBatchScreen: isNonBatchScreen);
                    return result?.FailedToGetTrapetsDataItems?.ToDictionary(x => x.CustomerId, x => x.Reason) ?? new Dictionary<int, string>();
                });

            customerClient
                .Setup(x => x.LoadSettings("documentClientData"))
                .Returns(new Dictionary<string, string>
                {
                    ["orgnr"] = "559040-6483",
                    ["name"] = "Testbolaget AB",
                    ["streetAddress"] = "Testgatan 99",
                    ["zipCode"] = "111 11",
                    ["postalArea"] = "Stockholm",
                    ["footerAddress"] = "Testgatan 99, 111 11 Stockholm | Org.nr: 559040-6483",
                    ["contactText"] = "Vid frågor maila nosuch@naktergal.tech.",
                    ["email"] = "nosuch@naktergal.tech",
                    ["website"] = "www.naktergal.tech",
                });

            /*
            Temporary simplified mock id provider. Replace with the actual MockSignatureService once we migrate that to core
             */
            customerClient
                .Setup(x => x.CreateElectronicIdSignatureSession(It.IsAny<SingleDocumentSignatureRequestUnvalidated>()))
                .Returns<SingleDocumentSignatureRequestUnvalidated>(request =>
                {
                    var sessionService = new SignatureSessionService(support.Clock, support.CreateCustomerContextFactory());
                    var session = SignatureSessionService.CreateLocalSessionFromRequest(request, "mock", support.ClientConfiguration);
                    using(var context = support.CreateCustomerContextFactory().CreateContext())
                    {
                        sessionService.StoreSession(session, context);
                        context.SaveChanges();
                    }
                    session.ProviderSessionId = Guid.NewGuid().ToString();
                    return session;
                });

            customerClient
                .Setup(x => x.GetElectronicIdSignatureSession(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns<string, bool>((sessionId, closeIfOpen) =>
                {
                    var sessionService = new SignatureSessionService(support.Clock, support.CreateCustomerContextFactory());
                    using (var context = support.CreateCustomerContextFactory().CreateContext())
                    {
                        var session = sessionService.GetSession(sessionId, context);
                        bool wasClosed = false;
                        if(session != null && closeIfOpen && !session.ClosedDate.HasValue)
                        {
                            session.ClosedDate = context.CoreClock.Today;
                            context.SaveChanges();
                            wasClosed = true;
                        }

                        return (Session: session, WasClosed: wasClosed);
                    }
                });

            return customerClient;
        }

        private class TestUrlService : IUrlService
        {
            public string ArchiveDocumentUrl(string archiveKey, bool setFilename)
            {
                return $"http://localhost/{archiveKey}";
            }

            public Uri GetCustomerRelationUrlOrNull(string relationType, string relationId)
            {
                return new Uri("http://localhost");
            }
        }

        public static KycManagementService CreateKycManagementService(SupportShared support)
        {
            var customerEnvSettings = new Mock<ICustomerEnvSettings>(MockBehavior.Strict);
            var customerContextFactory = new CustomerContextFactory(() => new CustomerContextExtended(support.CurrentUser, support.Clock));
            var templateService = new KycQuestionsTemplateService(customerContextFactory, customerEnvSettings.Object, support.ClientConfiguration);
            return new KycManagementService(
                    customerContextFactory, x => CreateCustomerRepository(x, support), new TestUrlService(), support.ClientConfiguration,
                    new Customer.Shared.Services.KycAnswersUpdateService(customerContextFactory, support.CurrentUser, support.Clock, templateService, support.CreateCachedSettingsService(),
                        support.EncryptionService));
        }

        public static CustomerWriteRepository CreateCustomerRepository(ICustomerContext context, SupportShared support) =>
            new CustomerWriteRepository(context, support.CurrentUser, support.Clock, support.EncryptionService, support.ClientConfiguration);

        public static NTechSimpleSettingsCore CreateCm1Settings(TemporaryDirectory tempDir)
        {
            FileInfo fileName = tempDir.GetRelativeTempFile("cm1-business-credit-settings.txt");
            File.WriteAllText(fileName.FullName, @"DestinationGuid={347985a3-4972-4f99-a2f0-acf879e27347}
 CreditProductType=24
 CreditTransactionTypeIncomingPayment=18
 CreditTransactionTypeOutgoingPayment=19
 SavingsProductType=25
 SavingsTransactionTypeIncomingPayment=18
 SavingsTransactionTypeOutgoingPayment=19
 Cm1AmlExportProfileNameCustomersCredits=Cm1Customers
 Cm1AmlExportProfileNameCustomersSavings=Cm1Customers
 Cm1AmlExportProfileNameTransactionsCredits=Cm1Transactions
 Cm1AmlExportProfileNameTransactionsSavings=Cm1Transactions
 SavingsRelationTypes=SavingsAccount_StandardAccount
 CreditsRelationTypes=Credit_UnsecuredLoan
 FileSuffix=FI
 LimitTransactions=5000
 CustomerSavingsProductType=Konsument inlåning
 CustomerCreditProductType=Konsument kredit");
            return NTechSimpleSettingsCore.ParseSimpleSettingsFile(fileName.FullName);
        }

        public static KycScreeningService CreateKycScreeningService(IServiceProvider provider, bool isPepHit, bool isSanctionHit) =>
            new KycScreeningService(
                (context, user) => new CustomerWriteRepository(context, user, provider.GetRequiredService<ICoreClock>(), 
                provider.GetRequiredService<EncryptionService>(), 
                provider.GetRequiredService<IClientConfigurationCore>()),
                provider.GetRequiredService<ICoreClock>(),
                new Lazy<IKycScreeningProviderServiceFactory>(() => new KycProviderFactory(isPepHit, isSanctionHit)),
                provider.GetRequiredService<IKycManagementService>(), provider.GetRequiredService<INTechCurrentUserMetadata>(),
                provider.GetRequiredService<CustomerContextFactory>(), provider.GetRequiredService<EncryptionService>(),
                provider.GetRequiredService<IClientConfigurationCore>(), provider.GetRequiredService<ICustomerEnvSettings>());

        private class KycProviderFactory : IKycScreeningProviderServiceFactory, IKycScreeningProviderService
        {
            private readonly bool isPepHit;
            private readonly bool isSanctionHit;

            public KycProviderFactory(bool isPepHit, bool isSanctionHit)
            {
                this.isPepHit = isPepHit;
                this.isSanctionHit = isSanctionHit;
            }

            public IKycScreeningProviderService CreateMultiCheckService() => this;
            
            public bool DoesCurrentProviderSupportContactInfo() => true;

            public IDictionary<string, List<KycScreeningListHit>> Query(List<KycScreeningQueryItem> items, KycScreeningListCode list = KycScreeningListCode.All) =>
                items.ToDictionary(x => x.ItemId, x => 
                    ((isPepHit || isSanctionHit) 
                        ? Enumerables.Singleton(new KycScreeningListHit { IsPepHit = true, IsSanctionHit = true })
                        : Enumerable.Empty<KycScreeningListHit>()).ToList());
        }
    }
}
