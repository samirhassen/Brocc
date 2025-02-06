using Microsoft.Extensions.DependencyInjection;
using Moq;
using nPreCredit;
using nPreCredit.Code;
using nPreCredit.Code.Clients;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.LegacyUnsecuredLoans;
using NTech.Core.Customer.Database;
using NTech.Core.Customer.Shared.Services;
using NTech.Core.Host.IntegrationTests.Shared.Services;
using NTech.Core.Host.IntegrationTests.UlLegacy;
using NTech.Core.Host.IntegrationTests.UlLegacy.Utilities;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.PreCredit.Shared.Services;
using NTech.Core.PreCredit.Shared.Services.UlLegacy;
using NTech.Core.PreCredit.Shared.Services.Utilities;
using NTech.Services.Infrastructure.Email;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlLegacyScenarioTests
    {
        private void AnswerAdditionalQuestions(UlLegacyTestRunner.TestSupport support)
        {
            var applicationNr = (string)support.Context["TestPerson1_ApplicationNr"];
            var testPerson1CustomerId = TestPersons.GetTestPersonCustomerIdBySeed(support, 1);

            var customerClient = TestPersons.CreateRealisticCustomerClient(support);

            var serviceRegistry = new Mock<INTechServiceRegistry>(MockBehavior.Strict);
            serviceRegistry
                .Setup(x => x.ExternalServiceUrl(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Tuple<string, string>[]>()))
                .Returns(new Uri("http://localhost"));
            var envSettings = support.PreCreditEnvSettings;
            var showInfoOnNextPageLoadService = new Mock<IShowInfoOnNextPageLoadService>(MockBehavior.Strict);
            var userNameService = new Mock<IUserDisplayNameService>(MockBehavior.Strict);
            var emailService = new Mock<INTechEmailService>(MockBehavior.Strict);
            var emailFactory = new Mock<INTechEmailServiceFactory>(MockBehavior.Strict);
            emailFactory.Setup(x => x.CreateEmailService()).Returns(emailService.Object);
            var publishingService = new Mock<IPublishEventService>(MockBehavior.Default);
            var partialCreditApplicationModelRepository = new PartialCreditApplicationModelRepository(support.EncryptionService, support.PreCreditContextService, new LinqQueryExpanderDoNothing());
            var questionsSender = new AdditionalQuestionsSender(support.CurrentUser, userNameService.Object, showInfoOnNextPageLoadService.Object, support.Clock,
                partialCreditApplicationModelRepository, support.PreCreditContextService, support.LoggingService, emailFactory.Object, publishingService.Object,
                customerClient.Object, envSettings);
            var signatureClient = new Mock<ISignicatSigningClientReadOnly>(MockBehavior.Strict);
            var workListService = new CreditManagementWorkListService(support.Clock, support.PreCreditContextService, envSettings, customerClient.Object);
            var creditClient = new Mock<ICreditClient>(MockBehavior.Strict);
            creditClient.Setup(x => x.NewCreditNumber()).Returns("L424242"); //TODO: Connect this to the credit module
            var agreementService = new UlLegacyAgreementSignatureService(support.Clock, support.PreCreditContextService, support.EncryptionService, partialCreditApplicationModelRepository,
                support.ClientConfiguration, creditClient.Object, customerClient.Object, support.CurrentUser);
            var applicationInfoService = new ApplicationInfoService(partialCreditApplicationModelRepository, envSettings, support.PreCreditContextService, customerClient.Object);
            var questionsService = new UlLegacyAdditionalQuestionsService(partialCreditApplicationModelRepository, support.PreCreditContextService, customerClient.Object, envSettings,
                support.ClientConfiguration, signatureClient.Object, workListService, agreementService, support.Clock, applicationInfoService, support.LoggingService,
                serviceRegistry.Object);
            questionsService.IsKycErrorHandlingSupressed = true;

            CreditApplicationOneTimeToken? questionsToken = null;
            using (var context = support.PreCreditContextService.CreateExtended())
            {
                questionsToken = context.CreditApplicationOneTimeTokensQueryable.Single(x => x.TokenType == "ApplicationWrapperToken1");
            }

            questionsService.ApplyAdditionalQuestionAnswers(questionsToken.Token, new UlLegacyKycAnswersModel
            {
                Iban = "FI4840541519568274",
                Applicant1 = new UlLegacyKycAnswersModel.Applicant
                {
                    ConsentRawJson = "{\"date\":\"11/27/2022, 3:51:47 PM\",\"applicantNr\":1,\"additionalQuestions_KfConsentText\":\"Din ansökan har godkänts preliminärt. För att vi ska kunna gå vidare med din ansökan behöver du svara på några kompletterande frågor. Vi har, precis som banker, en lagstadgad skyldighet att identifiera och känna våra kunder. Utöver personuppgifterna krävs tillräckliga uppgifter om bl.a. kundens verksamhet och ekonomiska ställning.\",\"additionalQuestions_KfLink\":{\"uri\":\"https://assets.ctfassets.net/6efv05ymvl3z/2i5j9akoOBeAmlpcWr8BBe/3f8962039d668b924832919d7f3bdbb5/Brocc_-_Tietosuojaka__yta__nto___-_Fi_-2022-09-16.pdf\",\"rawLinkText\":\"Information om hur Brocc Finance AB behandlar mina personuppgifter.\"},\"additionalQuestions_ConsentText\":{\"consentChecked\":true,\"text\":\"Jag bekräftar att jag har tagit del av uppgifterna ovan samt läst Brocc Finance AB:s information om hur Brocc Finance AB behandlar mina personuppgifter.\"}}"
                }
            }, "sv");

            AddAnswers(support);

            questionsService.GetApplicationState(questionsToken.Token);

            //Check that the expected customer properties got added
            using (var context = new CustomerContextExtended(support.CurrentUser, support.Clock))
            {
                var customerProperties = context
                    .CustomerProperties
                    .Where(x => x.CustomerId == testPerson1CustomerId && x.IsCurrentData)
                    .ToList();

                var expectedProperties = new HashSet<string>
                {
                    "includeInFatcaExport",
                    "taxcountries",
                    "citizencountries"
                };
                foreach (var expectedProperty in expectedProperties)
                {
                    Assert.That(customerProperties.Any(x => x.Name == expectedProperty), Is.True, $"Missing customer property {expectedProperty}");
                }
            }

            //Simulate create unsigned agreement
            var documentClient = new Mock<IDocumentClient>(MockBehavior.Strict);
            var pdfBuilder = new LoanAgreementPdfBuilder(support.Clock, partialCreditApplicationModelRepository, customerClient.Object,
                support.ClientConfiguration, new PreCredit.Shared.PreCreditContextFactory(support.PreCreditContextService.CreateExtended),
                support.LoggingService, support.PreCreditEnvSettings, documentClient.Object, new Dictionary<string, string>(), _ => new byte[] { 1, 2, 3 });
            var agreementPrintContext = pdfBuilder.CreateNewLoanAgreementPrintContext(applicationNr);

            Assert.That(agreementPrintContext.Keys.Count, Is.GreaterThan(0));

            using (var context = support.PreCreditContextService.CreateExtended())
            {
                //Check that the expected application items got added
                var expectedCreditApplicationItems = new HashSet<string>
                {
                    "consentRawJson"
                };

                var questionItemNames = context
                    .CreditApplicationItemsQueryable
                    .Where(x => x.ApplicationNr == applicationNr && x.GroupName == "question1").Select(x => x.Name)
                    .ToHashSetShared();

                foreach (var expectedApplicationItemName in expectedCreditApplicationItems)
                {
                    Assert.That(questionItemNames.Contains(expectedApplicationItemName), Is.True, $"Missing expected CreditApplicationItem {expectedApplicationItemName}");
                }

                var frequencyAnswer = SharedCustomer.CreateKycManagementService(support)
                    .FetchLatestCustomerQuestionsSet(testPerson1CustomerId)
                    ?.Items
                    ?.FirstOrDefault(x => x.QuestionCode == "loan_paymentfrequency");
                Assert.That(frequencyAnswer?.AnswerCode, Is.EqualTo("onschedule"), "Missing product questions");
            }

            using (var customerContext = new CustomerContext())
            {
                Assert.That(
                    customerContext.TrapetsQueryResults.Count(x => x.CustomerId == testPerson1CustomerId),
                    Is.EqualTo(1), "Customer was screened");
            }
        }

        private void AddAnswers(UlLegacyTestRunner.TestSupport support)
        {
            var applicationNr = (string)support.Context["TestPerson1_ApplicationNr"];
            var testPerson1CustomerId = TestPersons.GetTestPersonCustomerIdBySeed(support, 1);

            void AssertQuestionSetCount(int expectedCount)
            {
                using (var context = support.CreateCustomerContextFactory().CreateContext())
                {
                    Assert.That(context.StoredCustomerQuestionSetsQueryable.Count(), Is.EqualTo(expectedCount));
                }
            }

            AssertQuestionSetCount(0);

            var sessionService = support.Services.GetRequiredService<KycQuestionsSessionService>();
            var templateService = support.Services.GetRequiredService<KycQuestionsTemplateService>();
            var session = sessionService.CreateSession(new CreateKycQuestionSessionRequest
            {
                Language = "fi",
                QuestionsRelationType = "Credit_UnsecuredLoan",
                CustomerIds = new List<int> { testPerson1CustomerId },
                SlidingExpirationHours = 1,
                RedirectUrl = null,
                SourceType = "UnsecuredLoanApplication",
                SourceId = applicationNr,
                SourceDescription = "Addq"
            });

            var customerPagesSession = sessionService.LoadCustomerPagesSession(session.SessionId, templateService);

            sessionService.HandleSessionAnswers(new CustomerPagesHandleKycQuestionAnswersRequest
            {
                SessionId = session.SessionId,
                CustomerAnswers = new List<CustomerPagesHandleKycQuestionAnswersCustomer>
                {
                    new CustomerPagesHandleKycQuestionAnswersCustomer
                    {
                        CustomerKey = session.CustomerIdByCustomerKey.Single().Key,
                        Answers = KycAnswersUlLegacy.CreateApplicationAnswers(support)
                    }
                }
            });

            AssertQuestionSetCount(1);
        }
    }
}