using Moq;
using nPreCredit.Code.Balanzia;
using nPreCredit.Code.Services.LegacyUnsecuredLoans;
using nPreCredit.Code.Services;
using nPreCredit.Code;
using nPreCredit;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.PreCredit.Services;
using NTech.Core.PreCredit.Shared.Services;
using NTech.Services.Infrastructure.Email;
using NTech.Core.PreCredit.Database;
using Newtonsoft.Json;
using nPreCredit.Code.Scoring.BalanziaScoringRules;
using NTech.Core.PreCredit.Shared.Code.PetrusOnlyScoringService;
using NTech.Core.PreCredit.Shared.Services.UlLegacy;
using NTech.Core.Module.Infrastrucutre.HttpClient;
using nPreCredit.Code.Services.UnsecuredLoans;

namespace NTech.Core.Host.IntegrationTests.UlLegacy.Utilities
{
    internal class ApplicationsLegacy
    {
        public static string CreateApplication(UlLegacyTestRunner.TestSupport support, int personSeed,
            int requestedRepaymentTimeInYears,
            int requestedAmount)
        {
            var customerId = TestPersons.EnsureTestPerson(support, personSeed);
            var testPersonData = TestPersons.GetTestPersonDataBySeed(support, personSeed);

            var customerClient = TestPersons.CreateRealisticCustomerClient(support);

            var envSettings = support.PreCreditEnvSettings;
            var repo = new UnsecuredCreditApplicationProviderRepository(
                envSettings,
                support.Clock,
                support.ClientConfiguration,
                customerClient.Object,
                () => new CoreLegacyUnsecuredCreditApplicationDbWriter(support.CurrentUser, support.Clock, support.EncryptionService),
                new CreditApplicationKeySequenceGenerator(support.PreCreditContextService));

            var abTestingService = new Mock<IAbTestingService>(MockBehavior.Strict);
            abTestingService.Setup(x => x.AssignExperimentOrNull()).Returns<ApplicationAbTestExperimentModel>(null);
            abTestingService.Setup(x => x.GetVariationSetForApplication(It.IsAny<string>())).Returns(new EmptyVariationSet());

            var publishingService = new Mock<IPublishEventService>(MockBehavior.Default);

            var creationService = new BalanziaFiUnsecuredLoanApplicationCreationService(
                support.Clock,
                repo,
                publishingService.Object,
                new Mock<IAdServiceIntegrationService>(MockBehavior.Strict).Object,
                abTestingService.Object,
                support.ClientConfiguration,
                envSettings,
                customerClient.Object,
                support.CreateCachedSettingsService());

            var request = UlLegacyCreditApplicationRequestGenerator.CreateRequest(
                testPersonData["civicRegNr"],
                testPersonData["email"],
                testPersonData["phone"],
                requestedRepaymentTimeInYears: requestedRepaymentTimeInYears,
                requestedAmount: requestedAmount);
            

            Assert.True(
                creationService.TryCreateBalanziaFiLikeApplication(request, false, false, support.CurrentUser, out var failedMessage, out var applicationNr),
                failedMessage);
            
            publishingService.Verify(x => x.Publish(PreCreditEventCode.CreditApplicationCreated, It.IsAny<string>()), Times.Once());

            support.Context[$"Application_MainApplicantCustomerId_{applicationNr}"] = customerId;
            support.Context[$"Application_MainApplicantPersonSeed_{applicationNr}"] = personSeed;

            return applicationNr;
        }

        public static (bool IsAccepted, decimal? OfferedAmount, decimal? OfferedInterestRate, int? OfferedRepaymentTimeInMonths, List<string>? RejectionReasons)? DoAutomaticCreditCheckOnApplication_Accept(UlLegacyTestRunner.TestSupport support,
            string applicationNr,
            PetrusOnlyCreditCheckResponse.OfferModel offer) =>
            DoAutomaticCreditCheckOnApplicationWithPetrusTwo(support, applicationNr, (Offer: offer, RejectionReason: null));
        
        public static (bool IsAccepted, decimal? OfferedAmount, decimal? OfferedInterestRate, int? OfferedRepaymentTimeInMonths, List<string>? RejectionReasons)? DoAutomaticCreditCheckOnApplication_Reject(UlLegacyTestRunner.TestSupport support,
            string applicationNr,
            string rejectionReason) =>
            DoAutomaticCreditCheckOnApplicationWithPetrusTwo(support, applicationNr, (Offer: null, RejectionReason: rejectionReason));

        private static (bool IsAccepted, decimal? OfferedAmount, decimal? OfferedInterestRate, int? OfferedRepaymentTimeInMonths, List<string>? RejectionReasons)? DoAutomaticCreditCheckOnApplicationWithPetrusTwo(
            UlLegacyTestRunner.TestSupport support, string applicationNr,
            (PetrusOnlyCreditCheckResponse.OfferModel? Offer, string? RejectionReason) petrusResult)
        {
            var customerClient = TestPersons.CreateRealisticCustomerClient(support);
            customerClient
                .Setup(x => x.GetCustomerIdsWithSameAdress(It.IsAny<int>(), true))
                .Returns(new List<int>());

            var repo = support.GetRequiredService<UnsecuredCreditApplicationProviderRepository>();
            var envSettings = support.PreCreditEnvSettings;

            var abTestingService = new Mock<IAbTestingService>(MockBehavior.Strict);
            abTestingService.Setup(x => x.AssignExperimentOrNull()).Returns<ApplicationAbTestExperimentModel>(null);
            abTestingService.Setup(x => x.GetVariationSetForApplication(It.IsAny<string>())).Returns(new EmptyVariationSet());
            var publishingService = new Mock<IPublishEventService>(MockBehavior.Default);

            var partialCreditApplicationModelRepository = support.GetRequiredService<PartialCreditApplicationModelRepository>();
            var creditClient = new Mock<ICreditClient>(MockBehavior.Strict);
            creditClient
                .Setup(x => x.GetCustomerCreditHistory(It.IsAny<List<int>>()))
                .Returns(new List<HistoricalCreditExtended>());
            creditClient
                .Setup(x => x.GetCurrentReferenceInterest())
                .Returns(0.1m);
            var customerServiceRepo = new Mock<ICustomerServiceRepository>(MockBehavior.Strict);
            customerServiceRepo
                .Setup(x => x.FindByCustomerIds(It.IsAny<int[]>()))
                .Returns<int[]>(x => x.ToDictionary(x => x, x => new List<Banking.ScoringEngine.HistoricalApplication>()));
            var userNameService = new Mock<IUserDisplayNameService>(MockBehavior.Strict);
            var showInfoOnNextPageLoadService = new Mock<IShowInfoOnNextPageLoadService>(MockBehavior.Loose);
            var emailService = new Mock<INTechEmailService>(MockBehavior.Loose);
            var emailFactory = new Mock<INTechEmailServiceFactory>(MockBehavior.Strict);
            emailFactory.Setup(x => x.CreateEmailService()).Returns(emailService.Object);
            var questionsSender = new AdditionalQuestionsSender(support.CurrentUser, userNameService.Object, showInfoOnNextPageLoadService.Object, support.Clock,
                partialCreditApplicationModelRepository, support.PreCreditContextService, support.LoggingService, emailFactory.Object, publishingService.Object,
                customerClient.Object, envSettings);
            var urlService = new Mock<IServiceRegistryUrlService>(MockBehavior.Strict);
            var documentClient = new Mock<IDocumentClient>(MockBehavior.Strict);
            int documentNameCounter = 1;
            documentClient
                .Setup(x => x.ArchiveStore(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => $"document-{documentNameCounter++}");
            var commmentService = new ApplicationCommentService(support.PreCreditContextService, userNameService.Object, urlService.Object,
                documentClient.Object);
            var rejectionService = new LegacyUnsecuredLoansRejectionService(commmentService, support.Clock, support.PreCreditContextService,
                support.LoggingService, envSettings,
                emailService.Object, publishingService.Object, partialCreditApplicationModelRepository, customerClient.Object);
            var preCreditContextFactory = new PreCredit.Shared.PreCreditContextFactory(support.PreCreditContextService.CreateExtended);
            var campaignCode = new Mock<ICampaignCode>(MockBehavior.Strict);
            campaignCode.Setup(x => x.IsCodeThatForcesManualControl(It.IsAny<string>())).Returns(false);
            campaignCode.Setup(x => x.IsCodeThatRemovesInitialFee(It.IsAny<string>())).Returns(false);
            var referenceInterestService = new Mock<IReferenceInterestRateService>(MockBehavior.Strict);
            var randomNrScoringVariableGenerator = new Mock<IRandomNrScoringVariableProvider>(MockBehavior.Strict);
            randomNrScoringVariableGenerator.Setup(x => x.GenerateRandomNrBetweenOneAndOneHundred(It.IsAny<string>())).Returns(100);
            randomNrScoringVariableGenerator.Setup(x => x.GetRejectBelowLimit()).Returns(RandomNrScoringVariableProvider.RejectHalfCutOff);

            referenceInterestService.Setup(x => x.GetCurrent()).Returns(0.1m);
            var applicationInfoService = new ApplicationInfoService(partialCreditApplicationModelRepository, envSettings, support.PreCreditContextService, customerClient.Object);
            var applicationCheckPointService = new ApplicationCheckpointService(applicationInfoService, customerClient.Object, support.ClientConfiguration);

            var petrusFactory = new PetrusOnlyScoringServiceFactory(support.PreCreditEnvSettings, support.CreateCachedSettingsService(), new ServiceClientSyncConverterCore(), 
                documentClient.Object, commmentService);

            var petrusOnlyScoringService = new Mock<IPetrusOnlyScoringService>(MockBehavior.Strict);
                        
            petrusOnlyScoringService
                .Setup(x => x.NewCreditCheck(It.IsAny<PetrusOnlyCreditCheckRequest>()))
                .Returns<PetrusOnlyCreditCheckRequest>(request =>
                {
                    var customerId = partialCreditApplicationModelRepository.Get(applicationNr, applicantFields: new List<string> { "customerId" })
                        .Applicant(1).Get("customerId").IntValue.Required;
                    var testPersonData = TestPersons.GetTestPersonDataByCustomerId(support, customerId);
                    var response =  new PetrusOnlyCreditCheckResponse
                    {
                        Accepted = petrusResult.Offer != null,
                        LoanApplicationId = applicationNr + "_1",
                        MainApplicant = petrusResult.Offer == null ? null : new PetrusOnlyCreditCheckResponse.ApplicantModel
                        {
                            StreetAddress = testPersonData["addressStreet"],
                            City = testPersonData["addressCity"],
                            FirstName = testPersonData["firstName"],
                            LastName = testPersonData["lastName"],
                            ZipCode = testPersonData["addressZipcode"]
                        },
                        Offer = petrusResult.Offer,
                        RejectionReason = petrusResult.RejectionReason                       
                    };
                    return response;
                });

            petrusFactory.OverrideScoringService = petrusOnlyScoringService.Object;

            var creditCheckService = new PetrusOnlyCreditCheckService(partialCreditApplicationModelRepository, customerServiceRepo.Object,
                support.PreCreditEnvSettings, customerClient.Object, abTestingService.Object, creditClient.Object, preCreditContextFactory, applicationCheckPointService,
                support.Clock, petrusFactory, publishingService.Object, questionsSender, rejectionService, randomNrScoringVariableGenerator.Object, support.ClientConfiguration,
                referenceInterestService.Object);

            creditCheckService.AutomaticCreditCheck(applicationNr, false);

            return GetAutomaticCreditCheckResult(support, applicationNr);
        }

        private static (bool IsAccepted, decimal? OfferedAmount, decimal? OfferedInterestRate, int? OfferedRepaymentTimeInMonths, List<string>? RejectionReasons)? GetAutomaticCreditCheckResult(UlLegacyTestRunner.TestSupport support, string applicationNr)
        {
            (bool IsAccepted, decimal? OfferedAmount, decimal? OfferedInterestRate, int? OfferedRepaymentTimeInMonths, List<string>? RejectionReasons)? result = null;

            using var context = new PreCreditContext();
            var decision = context
                .CreditApplicationHeaders
                .Where(x => x.ApplicationNr == applicationNr)
                .Select(x => new
                {
                    x.CurrentCreditDecision
                })
                .Single()
                .CurrentCreditDecision;

            if (decision is AcceptedCreditDecision a)
            {
                var offer = JsonConvert.DeserializeAnonymousType(a.AcceptedDecisionModel, new
                {
                    offer = new
                    {
                        amount = (decimal?)null,
                        repaymentTimeInMonths = (int?)null,
                        marginInterestRatePercent = (decimal?)null
                    }
                })?.offer;
                result = (IsAccepted: true, OfferedAmount: offer?.amount, OfferedInterestRate: offer?.marginInterestRatePercent,
                    OfferedRepaymentTimeInMonths: offer?.repaymentTimeInMonths, RejectionReasons: (List<string>?)null);
            }
            else if (decision is RejectedCreditDecision d)
            {
                var rejectionReasons = JsonConvert.DeserializeAnonymousType(d.RejectedDecisionModel, new
                {
                    recommendation = new
                    {
                        RejectionReasons = (List<string>?)null
                    }
                })?.recommendation?.RejectionReasons;
                result = (IsAccepted: false, OfferedAmount: null, OfferedInterestRate: null,
                    OfferedRepaymentTimeInMonths: null, RejectionReasons: rejectionReasons);
            }
            else
            {
                Assert.Fail("No credit decision found");
            }
            return result;
        }

        public static PartialCreditReportModel CreateCreditReport(UlLegacyTestRunner.TestSupport support, int seedNr)
        {
            var testPerson = TestPersons.GetTestPersonDataBySeed(support, seedNr);
            var creditReportItems = testPerson.Keys.Select(propertyName =>
            {
                if (propertyName.StartsWith("creditreport_"))
                {
                    return new PartialCreditReportModel.Item
                    {
                        Name = propertyName.Substring("creditreport_".Length),
                        Value = testPerson[propertyName]
                    };
                }
                else if (propertyName.StartsWith("satfi_"))
                {
                    return new PartialCreditReportModel.Item
                    {
                        Name = propertyName.Substring("satfi_".Length),
                        Value = testPerson[propertyName]
                    };
                }
                else if (propertyName.IsOneOf("firstName", "lastName", "addressStreet", "addressZipcode", "addressCity", "addressCountry"))
                {
                    return new PartialCreditReportModel.Item
                    {
                        Name = propertyName,
                        Value = testPerson[propertyName]
                    };
                }
                else
                {
                    return null;
                }
            }).Where(x => x != null).ToList();

            return new PartialCreditReportModel(new List<PartialCreditReportModel.Item>(creditReportItems!));
        }
    }
}
