using Moq;
using nPreCredit;
using nPreCredit.Code;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.NewUnsecuredLoans;
using nPreCredit.Code.Services.SharedStandard;
using nPreCredit.Code.StandardPolicyFilters;
using nPreCredit.Code.StandardPolicyFilters.DataSources;
using NTech.Banking.PluginApis.CreateApplication;
using NTech.Core.Host.IntegrationTests.UlStandard;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.PreCredit.Database;
using NTech.Core.PreCredit.Shared;
using NTech.Core.PreCredit.Shared.Models;
using NTech.Core.PreCredit.Shared.Services;
using NTech.Core.PreCredit.Shared.Services.Utilities;
using NTech.Services.Infrastructure.CreditStandard;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlStandardLikeScenarioTests
    {
        private void AddAndScoreApplicationOnTestPersonOne(UlStandardTestRunner.TestSupport support)
        {
            var customerId = TestPersons.EnsureTestPerson(support, 1);
            var f = new PreCreditContextFactory(() => new PreCreditContextExtended(support.CurrentUser, support.Clock));
            var s = support.GetRequiredService<PolicyFilterService>();

            //////////////////////////////////////////////////////////////////////
            //Simulate the user going into the ui, adding a new policy filter, adding just one rule banning unemployed and setting that as active
            //////////////////////////////////////////////////////////////////////
            var newSet = s.CreateOrEditPolicyFilterSet(new CreateOrEditPolicyFilterSetRequest
            {
                NewPending = new CreateOrEditPolicyFilterSetRequest.NewPendingModel
                {
                    UseGeneratedName = true
                }
            });

            var allRules = s.FetchPolicyFilterRuleSets(new FetchPolicyFilterRuleSetsRequest { IncludeAllRules = true }).AllRules;
            var bannedEmploymentRule = allRules.Single(x => x.RuleName == "BannedEmployment");

            var storedRejectionReasonName = (string?)"minimumDemands";
            var ruleSet = new RuleSet
            {
                InternalRules = new RuleAndStaticParameterValues[]
                {
                    new RuleAndStaticParameterValues(bannedEmploymentRule.RuleName, StaticParameterSet.CreateEmpty().SetStringList(
                        "bannedEmploymentFormCodes", new List<string>
                        {
                            CreditStandardEmployment.Code.unemployed.ToString()
                        }), storedRejectionReasonName)
                }
            };

            s.CreateOrEditPolicyFilterSet(new CreateOrEditPolicyFilterSetRequest
            {
                UpdateExisting = new CreateOrEditPolicyFilterSetRequest.UpdateExistingModel
                {
                    Id = newSet.Id,
                    RuleSet = ruleSet
                }
            });

            s.ChangePolicyFilterSetSlot(newSet.Id, "A");

            //Unemployed rejected
            {
                var applicationNr = CreateApplication(support, 1, employmentStatus: CreditStandardEmployment.Code.unemployed.ToString());
                var recommendation = NewCreditCheck(support, applicationNr);
                Assert.That(recommendation.PolicyFilterResult.IsAcceptRecommended, Is.EqualTo(false));
                Assert.That(recommendation.PolicyFilterResult.RejectionReasonNames, Is.SupersetOf(Enumerables.Singleton("minimumDemands")));
            }

            //Student accepted
            {
                var applicationNr = CreateApplication(support, 1, employmentStatus: CreditStandardEmployment.Code.student.ToString());
                var recommendation = NewCreditCheck(support, applicationNr);
                Assert.That(recommendation.PolicyFilterResult.IsAcceptRecommended, Is.EqualTo(true));
                Assert.That(recommendation.PolicyFilterResult.RejectionReasonNames, Is.Null);
                support.Context["AcceptedApplicationNr"] = applicationNr;
            }
        }

        private (
            Func<NewCreditCheckUlStandardService> NewCreditCheckService,
            Func<CreateApplicationUlStandardService> CreateApplicationService,
            Mock<ICustomerClient> CustomerClient) GetApplicationServiceFactories(UlStandardTestRunner.TestSupport support)
        {
            var documentClient = new Mock<IDocumentClient>(MockBehavior.Strict);
            var campaignService = new Mock<ICampaignCodeService>(MockBehavior.Strict);
            campaignService
                .Setup(x => x.MatchCampaignOnCreateApplication(It.IsAny<List<CreateApplicationRequestModel.ComplexItem>>()))
                .Returns(new List<CreateApplicationRequestModel.ComplexItem>());
            var relationSevice = new Mock<ILoanStandardCustomerRelationService>(MockBehavior.Loose);
            var partialCreditApplicationModelRepository = new PartialCreditApplicationModelRepository(support.EncryptionService, support.PreCreditContextService, LinqQueryExpanderDoNothing.SharedInstance);
            var customerClient = TestPersons.CreateRealisticCustomerClient(support);

            var creditApplicationCustomEditableFieldsService = new CreditApplicationCustomEditableFieldsService(new Lazy<int>(2));
            var applicationInfoService = new ApplicationInfoService(partialCreditApplicationModelRepository, support.PreCreditEnvSettings, support.PreCreditContextService, customerClient.Object);
            var dataSourceService = ApplicationDataSourceService.Create(creditApplicationCustomEditableFieldsService,
                support.PreCreditContextService, support.EncryptionService, applicationInfoService, customerClient.Object);
            var listService = new CreditApplicationListService(support.PreCreditContextService);
            var workflowService = new UnsecuredLoanStandardWorkflowService(support.PreCreditContextService, listService);
            var keyValueStoreService = new KeyValueStoreService(support.PreCreditContextService);
            var customerListService = new CreditApplicationCustomerListService(support.PreCreditContextService);
            var sharedCreateApplicationService = new SharedCreateApplicationService(support.PreCreditContextService, support.PreCreditEnvSettings,
                support.EncryptionService, documentClient.Object, campaignService.Object, customerListService, relationSevice.Object);

            var applicationService = support.GetRequiredService<CreateApplicationUlStandardService>();

            var ltlDataTables = new Mock<ILtlDataTables>(MockBehavior.Strict);
            ltlDataTables.Setup(x => x.IncomeTaxMultiplier).Returns(new decimal?());
            ltlDataTables.Setup(x => x.DefaultChildAgeInYears).Returns(new int?());
            ltlDataTables.Setup(x => x.DefaultApplicantAgeInYears).Returns(new int?());
            ltlDataTables.Setup(x => x.GetIndividualAgeCost(It.IsAny<int>())).Returns(1000m);
            ltlDataTables.Setup(x => x.GetHouseholdMemberCountCost(It.IsAny<int>())).Returns<int>(x => x * 1000m);
            ltlDataTables.Setup(x => x.StressInterestRatePercent).Returns(20m);
            ltlDataTables.Setup(x => x.CreditsUse360DayInterestYear).Returns(false);

            var creditReportService = support.GetRequiredService<LoanApplicationCreditReportService>();
            var ltlService = new nPreCredit.Code.Services.UnsecuredLoans.UnsecuredLoanLtlAndDbrService(support.PreCreditContextService, support.ClientConfiguration, customerClient.Object, ltlDataTables.Object);
            var creditClient = new Mock<ICreditClient>(MockBehavior.Strict);
            var creditReportClient = new Mock<ICreditReportClient>(MockBehavior.Strict);
            creditReportClient
                .Setup(x => x.FindCreditReportsByReason(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(new List<FindForCustomerCreditReportModel>());

            var dataSourceFactory = new UnsecuredLoanStandardApplicationPolicyFilterDataSourceFactory(ltlService, support.Clock, creditReportService, support.PreCreditContextService, customerClient.Object,
                creditClient.Object, creditReportClient.Object, support.ClientConfiguration, ltlDataTables.Object);

            var creditCheckService = new NewCreditCheckUlStandardService(applicationInfoService,
                ltlService, support.PreCreditContextService, dataSourceFactory, support.ClientConfiguration);

            return (NewCreditCheckService: () => creditCheckService, CreateApplicationService: () => applicationService, CustomerClient: customerClient);
        }

        private UnsecuredLoanStandardCreditRecommendationModel NewCreditCheck(UlStandardTestRunner.TestSupport support, string applicationNr)
        {
            var factories = GetApplicationServiceFactories(support);
            return factories.NewCreditCheckService().NewCreditCheck(applicationNr);
        }

        private string CreateApplication(UlStandardTestRunner.TestSupport support, int personSeed, string employmentStatus = "unemployed")
        {
            var factories = GetApplicationServiceFactories(support);
            var customerId = TestPersons.EnsureTestPerson(support, personSeed);
            var personData = TestPersons.GetTestPersonDataBySeed(support, personSeed);

            factories.CustomerClient
                .Setup(x => x.CreateOrUpdatePerson(It.IsAny<CreateOrUpdatePersonRequest>()))
                .Returns(customerId);

            var request = new UlStandardApplicationRequest
            {
                LoansToSettleAmount = 42000,
                RequestedAmount = 59000,
                RequestedRepaymentTimeInMonths = 12,
                HousingCostPerMonthAmount = 2000,
                HousingType = "tenant",
                NrOfHouseholdChildren = 4,
                Applicants = new List<UlStandardApplicationRequest.ApplicantModel>
                {
                    new UlStandardApplicationRequest.ApplicantModel
                    {
                        CivilStatus = "co_habitant",
                        ClaimsToBePep = false,
                        ClaimsToHaveKfmDebt = false,
                        EmployedSince = new DateTime(1994,12,10),
                        EmployerName = "Företaget AB",
                        EmployerPhone = "010 111 222 333",
                        EmploymentStatus = employmentStatus,
                        HasConsentedToCreditReport = true,
                        HasConsentedToShareBankAccountData = true,
                        MonthlyIncomeAmount = 36000,
                        
                        CivicRegNr = personData["civicRegNr"]
                    }
                },
                Meta = new UlStandardApplicationRequest.MetadataModel
                {
                    ProviderName = "self"
                }
            };

            return factories.CreateApplicationService().CreateApplication(request, isFromInsecureSource: false, requestJson: null);
        }
    }
}