using Moq;
using NTech.Core.Customer.Database;
using NTech.Core.Customer.Shared;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Customer.Shared.Models;
using NTech.Core.Customer.Shared.Services;
using NTech.Core.Host.IntegrationTests.UlLegacy;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlLegacyScenarioTests
    {
        private void EditKycQuestionTemplates(UlLegacyTestRunner.TestSupport support)
        {
            var contextFactory = new CustomerContextFactory(() => new CustomerContextExtended(support.CurrentUser, support.Clock));
            var customerSettings = new Mock<ICustomerEnvSettings>(MockBehavior.Strict);
            customerSettings.Setup(x => x.DefaultKycQuestionsSets).Returns(new Dictionary<string, KycQuestionsTemplate>());

            var service = new KycQuestionsTemplateService(contextFactory, customerSettings.Object, support.ClientConfiguration);

            var data1 = service.GetInitialData();

            Assert.That(
                data1.ActiveProducts.Select(x => x.RelationType).ToHashSet(),
                Is.EqualTo(new HashSet<string> { "Credit_UnsecuredLoan", "SavingsAccount_StandardAccount" }));

            KycQuestionsTemplate CreateSampleQuestions() =>
                    new KycQuestionsTemplate
                    {
                        Questions = new List<KycQuestionsTemplate.KycUiQuestion>
                        {
                            new KycQuestionsTemplate.KycUiQuestion
                            {
                                Key = "question1",
                                Type = "dropdown",
                                HeaderTranslations = new Dictionary<string, string>
                                {
                                    { "sv", "Question 1" },
                                    { "fi", "Questioni 1" }
                                },
                                Options = new List<KycQuestionsTemplate.KycUiQuestion.Option>
                                {
                                    new KycQuestionsTemplate.KycUiQuestion.Option
                                    {
                                        Value = "option1",
                                        Translations = new Dictionary<string, string>
                                        {
                                            { "sv", "Option 1" },
                                            { "fi", "Optioni 1" }
                                        },
                                    }
                                }
                            }
                        }
                    };

            var saveResponse = service.SaveQuestions(new SaveQuestionsRequest
            {
                RelationType = "Credit_UnsecuredLoan",
                ModelData = CreateSampleQuestions().Serialize()
            });

            var loadedData = service.GetModelData(new GetModelDataRequest { Id = saveResponse.Id })?.ModelData;

            var expectedQuestions = CreateSampleQuestions();
            expectedQuestions.Version = saveResponse.Version;

            Assert.That(loadedData, Is.EqualTo(expectedQuestions.Serialize()));
        }
    }
}
