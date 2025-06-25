using Moq;
using nCustomer;
using Newtonsoft.Json;
using NTech.Banking.CivicRegNumbers.Fi;
using NTech.Core.Customer.Database;
using NTech.Core.Customer.Shared.Services;
using NTech.Core.Savings.Shared.Database;
using NTech.Core.Savings.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NTech.Core.Savings.Database;
using NTech.Core.Savings.Shared.BusinessEvents;
using NTech.Core.Savings.Shared.Services;

namespace NTech.Core.Host.IntegrationTests.UlLegacy.Savings
{
    public partial class SavingsFiHappyFlowTests
    {

        private void CreateSavingsAccountTest(UlLegacyTestRunner.TestSupport support, bool isFrozen)
        {
            var customerClient = TestPersons.CreateRealisticCustomerClient(support);

            var contextFactory = new SavingsContextFactory(() => new SavingsContext(), SavingsContext.IsConcurrencyException);
            var keyValueStore = new Mock<IKeyValueStoreService>(MockBehavior.Strict);            
            var mgr = new CreateSavingsAccountBusinessEventManager(support.CurrentUser, support.Clock, keyValueStore.Object, support.SavingsEnvSettings,
                support.ClientConfiguration, customerClient.Object, contextFactory);
            var mergeService = new Mock<ICustomerRelationsMergeService>(MockBehavior.Strict);
            Action<(string SavingsAccountNr, ISavingsContext Context, string SendingLocation)> sendWelcomeEmail = x =>
                TestContext.WriteLine("Welcome email: " + JsonConvert.SerializeObject(new { x.SavingsAccountNr, x.SendingLocation }, Formatting.Indented));
            var s = new SavingsAccountCreationService(support.Clock, support.LoggingService, customerClient.Object, sendWelcomeEmail, _ => true,
                mgr, contextFactory, mergeService.Object);

            var request = CreateAccountRequest();

            if (isFrozen)
            {
                var otherTestPersonCustomerId = TestPersons.EnsureTestPerson(support, 2);
                using (var context = support.CreateCustomerContextFactory().CreateContext())
                {
                    context.BeginTransaction();
                    var repo = new CustomerWriteRepository(context, context.CurrentUser, context.CoreClock, support.EncryptionService, support.ClientConfiguration);
                    repo.UpdateProperties(new List<CustomerPropertyModel>
                {
                    new CustomerPropertyModel
                    {
                        CustomerId = otherTestPersonCustomerId,
                        Group = "whatever",
                        Name = "email",
                        Value = request.ApplicationItems.Single(x => x.Name == "customerEmail").Value
                    },
                    new CustomerPropertyModel
                    {
                        CustomerId = otherTestPersonCustomerId,
                        Group = "whatever",
                        Name = "phone",
                        Value = request.ApplicationItems.Single(x => x.Name == "customerPhone").Value
                    }
                }, true);
                    context.SaveChanges();
                    context.CommitTransaction();
                }
            }

            int customerId;
            using (var context = support.CreateCustomerContextFactory().CreateContext())
            {
                customerId = CustomerIdSourceCore.GetCustomerIdByCivicRegNr(CivicRegNumberFi.Parse(request.ApplicationItems.Single(x => x.Name == "customerCivicRegNr").Value), context);
            }

            var kycQuestionsTemplateService = new KycQuestionsTemplateService(support.CreateCustomerContextFactory(), support.CustomerEnvSettings, support.ClientConfiguration);
            var kycAnswersUpdateService = new KycAnswersUpdateService(support.CreateCustomerContextFactory(), support.CurrentUser, support.Clock, kycQuestionsTemplateService,
                support.CreateCachedSettingsService(), support.EncryptionService);

            kycAnswersUpdateService.AddCustomerQuestionsSet(new Module.Shared.Clients.CustomerQuestionsSet
            {
                CustomerId = customerId,
                AnswerDate = support.Clock.Now.DateTime,
                Items = new List<Module.Shared.Clients.CustomerQuestionsSetItem>
                {
                    new Module.Shared.Clients.CustomerQuestionsSetItem
                    {
                        QuestionCode = "nrdepositsperyearrangeestimate",
                        QuestionText = "Hur ofta kommer insättningar att göras per år?",
                        AnswerCode = "0_10",
                        AnswerText = "Färre än 10 gånger"
                    },
                    new Module.Shared.Clients.CustomerQuestionsSetItem
                    {
                        QuestionCode = "savingshorizonestimate",
                        QuestionText = "Hur länge har du tänkt spara?",
                        AnswerCode = "morethanfiveyears",
                        AnswerText = "Långsiktigt (mer än 5 år)"
                    },
                    new Module.Shared.Clients.CustomerQuestionsSetItem
                    {
                        QuestionCode = "initialdepositrangeestimate",
                        QuestionText = "Vilket ungefärligt värde kommer du att överföra i samband med öppnandet?",
                        AnswerCode = "0_100",
                        AnswerText = "Mindre än 100 €"
                    },
                    new Module.Shared.Clients.CustomerQuestionsSetItem
                    {
                        QuestionCode = "mainoccupation",
                        QuestionText = "Vilken är din huvudsakliga sysselsättning?",
                        AnswerCode = "manager",
                        AnswerText = "Chefsjobb"
                    },
                    new Module.Shared.Clients.CustomerQuestionsSetItem
                    {
                        QuestionCode = "purpose",
                        QuestionText = "Vad är det huvudsakliga syftet med ditt sparande?",
                        AnswerCode = "pension",
                        AnswerText = "Pension"
                    },
                    new Module.Shared.Clients.CustomerQuestionsSetItem
                    {
                        QuestionCode = "sourceoffunds",
                        QuestionText = "Varifrån kommer de pengar som du sätter in på kontot huvudsakligen ifrån?",
                        AnswerCode = "salaryorpension",
                        AnswerText = "Lön eller pension"
                    },
                    new Module.Shared.Clients.CustomerQuestionsSetItem
                    {
                        QuestionCode = "ispep",
                        QuestionText = "Har du en hög politisk befattning inom staten, är en nära släktning eller medarbetare till en sådan person?",
                        AnswerCode = isFrozen ? "true" : "false",
                        AnswerText = "Nej"
                    },
                    new Module.Shared.Clients.CustomerQuestionsSetItem
                    {
                        QuestionCode = "hasOtherTaxCountry",
                        QuestionText = "Är du skattepliktig i något annat land än Finland?",
                        AnswerCode = "false",
                        AnswerText = "Nej"
                    },
                    new Module.Shared.Clients.CustomerQuestionsSetItem
                    {
                        QuestionCode = "hasOtherCitizenCountry",
                        QuestionText = "Är du medborgare i något annat land än Finland?",
                        AnswerCode = "false",
                        AnswerText = "Nej"
                    },
                }
            },
            "SavingsAccount_StandardAccount", request.ApplicationItems.Single(x => x.Name == "savingsAccountNr").Value);

            var savingsAccountNr = s.CreateAccount(request)?.savingsAccountNr;

            using (var context = contextFactory.CreateContext())
            {
                var account = context
                    .SavingsAccountHeadersQueryable
                    .Where(x => x.SavingsAccountNr == savingsAccountNr)
                    .SingleOrDefault();
                Assert.That(account?.Status, Is.EqualTo(isFrozen ? "FrozenBeforeActive" : "Active"));
            }


            if (!isFrozen)
            {
                using (var customerContext = new CustomerContext())
                {
                    //Make sure tax customer and pep/sanction were auto populated
                    var taxPropertyNames = new List<string>
                {
                    "citizencountries",
                    "taxcountries",
                    "includeInFatcaExport",
                    "localIsSanction",
                    "localIsPep"
                };
                    var taxPropertyCount = customerContext
                        .CustomerPropertiesQueryable
                        .Count(x => x.CustomerId == customerId && x.IsCurrentData && taxPropertyNames.Contains(x.Name));
                    Assert.That(taxPropertyCount, Is.EqualTo(taxPropertyNames.Count), "Missing tax/citizen defaults");
                }
            }

            using (var savingsContext = new SavingsContext())
            {
                var remarkCodes = savingsContext.SavingsAccountCreationRemarks.Select(x => x.RemarkCategoryCode).ToHashSetShared();

                //Make sure the account was frozen due to same email and phone
                Assert.That(remarkCodes.Contains("FraudCheckSameEmail"), isFrozen ? Is.True : Is.False, isFrozen ? "Should be frozen due to same email on other customer" : "Should not be frozen");
                Assert.That(remarkCodes.Contains("FraudCheckSamePhone"), isFrozen ? Is.True : Is.False, isFrozen ? "Should be frozen due to same phone on other customer" : "Should not be frozen");
                Assert.That(remarkCodes.Contains("KycAttentionNeeded"), isFrozen ? Is.True : Is.False, isFrozen ? "Should be frozen due kyc question/screening mismatch" : "Should not be frozen");
            }
        }

        private CreateSavingsAccountRequest CreateAccountRequest()
        {
            return new CreateSavingsAccountRequest
            {
                ApplicationItems = new Dictionary<string, string>
                {
                    ["customerEmail"] = "test112313@example.org",
                    ["customerPhone"] = "3242354",
                    ["withdrawalIban"] = "FI5340550360392430",
                    ["customerCivicRegNr"] = "091082-730K",
                    ["savingsAccountNr"] = "S20030",
                    ["customerFirstName"] = "Fredrik",
                    ["customerLastName"] = "Hård",
                    ["customerNameSourceTypeCode"] = "TrustedParty",
                    ["customerAddressStreet"] = "Pesolantie 94859",
                    ["customerAddressZipcode"] = "21200",
                    ["customerAddressCity"] = "VANTAA",
                    ["customerAddressCountry"] = null!,
                    ["customerAddressSourceTypeCode"] = "TrustedParty",
                    ["signedAgreementDocumentArchiveKey"] = "16f3a357-0d2b-46c2-bc16-4476d8477e1f.pdf",
                    ["savingsAccountTypeCode"] = "StandardAccount"
                }.Select(x => new CreateSavingsAccountItem { Name = x.Key, Value = x.Value }).ToList()
            };
        }
    }
}
