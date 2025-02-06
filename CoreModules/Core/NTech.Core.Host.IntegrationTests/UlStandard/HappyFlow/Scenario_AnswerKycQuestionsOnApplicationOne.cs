using Microsoft.Extensions.DependencyInjection;
using Moq;
using nPreCredit.Code.Services;
using nPreCredit;
using NTech.Core.Customer.Shared.Services;
using NTech.Core.Host.IntegrationTests.UlStandard;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module;
using NTech.Core.PreCredit.Shared.Services.UlStandard.ApplicationAutomation;
using NTech.Core.PreCredit.Shared.Services.UlStandard;
using NTech.Core.PreCredit.Shared.Services.Utilities;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlStandardLikeScenarioTests
    {
        private void AnswerKycQuestionsOnApplicationOne(UlStandardTestRunner.TestSupport support)
        {
            var applicationNr = (string)support.Context["AcceptedApplicationNr"];
            KycStepAutomation.DisableErrorSupression = true;
            var customerClient = TestPersons.CreateRealisticCustomerClient(support);
            var serviceRegistry = new Mock<INTechServiceRegistry>(MockBehavior.Strict);
            serviceRegistry
                .Setup(x => x.ExternalServiceUrl("nCustomerPages", It.IsAny<string>(), It.IsAny<Tuple<string, string>[]>())).Returns(new Uri("http://localhost/something"));

            var kycSession = support.Services.GetRequiredService<UnsecuredLoanStandardApplicationKycQuestionSessionService>()
                .CreateSession(applicationNr, null);
            var kycSessionService = support.Services.GetRequiredService<KycQuestionsSessionService>();
            support.Services.GetRequiredService<KycQuestionsSessionService>()
                .HandleSessionAnswers(new CustomerPagesHandleKycQuestionAnswersRequest
                {
                    CustomerAnswers = kycSession.CustomerIdByCustomerKey.Select(x => new CustomerPagesHandleKycQuestionAnswersCustomer
                    {
                        CustomerKey = x.Key,
                        Answers = new List<CustomerQuestionsSetItem>()
                        {
                            new CustomerQuestionsSetItem
                            {
                                AnswerCode = "answer1",
                                AnswerText = "Text 1",
                                QuestionCode = "question1",
                                QuestionText = "Question 1"
                            }
                        }
                    }).ToList(),
                    SessionId = kycSession.SessionId
                });

            using (var customerContext = support.CreateCustomerContextFactory().CreateContext())
            {
                var customerId = kycSession.CustomerIdByCustomerKey.Values.First();
                Assert.That(
                    customerContext.TrapetsQueryResultsQueryable.Count(x => x.CustomerId == customerId),
                    Is.EqualTo(1));
                Assert.That(
                    customerContext.StoredCustomerQuestionSetsQueryable.Count(x => x.CustomerId == customerId),
                    Is.EqualTo(1));                
            }
        }
    }
}