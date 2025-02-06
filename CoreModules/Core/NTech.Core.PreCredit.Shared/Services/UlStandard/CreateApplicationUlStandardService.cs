using Newtonsoft.Json.Linq;
using nPreCredit.Code.Plugins;
using nPreCredit.WebserviceMethods.SharedStandard;
using NTech.Banking.CivicRegNumbers;
using NTech.Banking.PluginApis.CreateApplication;
using NTech.Core;
using NTech.Core.Module;
using NTech.Core.Module.Shared;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Core.PreCredit.Shared.Models;
using NTech.Services.Infrastructure.CreditStandard;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace nPreCredit.Code.Services.NewUnsecuredLoans
{
    public class CreateApplicationUlStandardService
    {
        private readonly UnsecuredLoanStandardWorkflowService workflowService;
        private readonly ICoreClock clock;
        private readonly ICustomerClient customerClient;
        private readonly IKeyValueStoreService keyValueStoreService;
        private readonly ApplicationDataSourceService applicationDataSourceService;
        private readonly SharedCreateApplicationService sharedCreateApplicationService;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly IPreCreditContextFactoryService preCreditContextFactoryService;
        private readonly IPreCreditEnvSettings envSettings;
        private readonly INTechEnvironment environment;
        private readonly IServiceClientSyncConverter syncConverter;
        private readonly ILoggingService loggingService;
        private readonly IDocumentClient documentClient;
        private static Lazy<FewItemsCache> cache = new Lazy<FewItemsCache>(() => new FewItemsCache());

        public static bool IsEnabled(IPreCreditEnvSettings envSettings) => envSettings.IsStandardUnsecuredLoansEnabled;

        public CreateApplicationUlStandardService(UnsecuredLoanStandardWorkflowService workflowService, ICoreClock clock, ICustomerClient customerClient, IKeyValueStoreService keyValueStoreService,
            ApplicationDataSourceService applicationDataSourceService, SharedCreateApplicationService sharedCreateApplicationService, IClientConfigurationCore clientConfiguration,
            IPreCreditContextFactoryService preCreditContextFactoryService, IPreCreditEnvSettings envSettings, INTechEnvironment environment, IServiceClientSyncConverter syncConverter,
            ILoggingService loggingService, IDocumentClient documentClient)
        {
            this.workflowService = workflowService;
            this.clock = clock;
            this.customerClient = customerClient;
            this.keyValueStoreService = keyValueStoreService;
            this.applicationDataSourceService = applicationDataSourceService;
            this.sharedCreateApplicationService = sharedCreateApplicationService;
            this.clientConfiguration = clientConfiguration;
            this.preCreditContextFactoryService = preCreditContextFactoryService;
            this.envSettings = envSettings;
            this.environment = environment;
            this.syncConverter = syncConverter;
            this.loggingService = loggingService;
            this.documentClient = documentClient;
        }

        private AffiliateModel GetAffiliateModelOrNull(string providerName) => envSettings.GetAffiliateModel(providerName, allowMissing: true);

        public string CreateApplication(UlStandardApplicationRequest request, bool isFromInsecureSource, string requestJson)
        {
            request = request ?? new UlStandardApplicationRequest();

            if (!(request.Applicants.Count == 1 || request.Applicants.Count == 2))
                throw new NTechCoreWebserviceException("Applicants must have one or two members") { IsUserFacing = true, ErrorCode = "invalidApplicantCount" };

            if (!string.IsNullOrWhiteSpace(request?.Meta?.ProviderName) && GetAffiliateModelOrNull(request.Meta.ProviderName) == null)
                throw new NTechCoreWebserviceException("Meta.ProviderName unknown provider used") { IsUserFacing = true, ErrorCode = "unknownProviderName" };

            if (request.LoansToSettleAmount.HasValue && request.HouseholdOtherLoans != null && request.HouseholdOtherLoans.Count > 0)
                throw new NTechCoreWebserviceException("LoansToSettleAmount and HouseholdOtherLoans cannot be combined. Use one or the other.") { IsUserFacing = true, ErrorCode = "householdOtherLoansCombined" };

            CreateApplicationRequestModelExtended.CheckForDuplicateCivicRegNrs(request?.Applicants?.Select(x => x.CivicRegNr), clientConfiguration);

            ValidatDataShare(request, isFromInsecureSource);

            var createApplicationContext = new PluginApplicationRequestTranslatorBase.Context(
                clock,
                customerClient,
                workflowService,
                keyValueStoreService,
                applicationDataSourceService,
                "UL",
                envSettings.GetAffiliateModels,
                preCreditContextFactoryService);

            var createRequest = TranslateRequest(request, createApplicationContext);

            PopulateDataShare(request, createRequest.NrOfApplicants, createRequest.ApplicationNr, (applicantNr, name, value) =>
            {
                var applicantList = createRequest.ComplexApplicationItems.Single(x => x.ListName == "Applicant" && x.Nr == applicantNr);
                applicantList.UniqueValues[name] = value;
            });

            if (requestJson != null)
                createApplicationContext.SetKeyValueStoreValue(createRequest.ApplicationNr, "ApplicationRequestJson", requestJson);

            var application = sharedCreateApplicationService.CreateApplication(
                createRequest, CreditApplicationTypeCode.unsecuredLoan, workflowService, CreditApplicationEventCode.CreditApplicationCreated);

            return application.ApplicationNr;
        }

        private CreateApplicationRequestModelExtended TranslateRequest(UlStandardApplicationRequest request, IApplicationCreationContext creationContext)
        {
            var createRequest = new CreateApplicationRequestModelExtended();

            var applicationItems = new Dictionary<string, string>();
            var applicantItems = new Dictionary<int, Dictionary<string, string>>();

            void AddApplicationItem(string name, string value) => applicationItems[name] = value;

            createRequest.ApplicationNr = creationContext.GenerateNewApplicationNr();
            createRequest.ProviderName = request.Meta.ProviderName;
            createRequest.NrOfApplicants = request.Applicants.Count;

            AddApplicationItem("requestedLoanAmount", request.RequestedAmount?.ToString());

            if (request.LoansToSettleAmount.HasValue || (request.HouseholdOtherLoans != null && request.HouseholdOtherLoans.Count > 0))
            {
                if (request.LoansToSettleAmount.HasValue)
                {
                    createRequest.AddComplexApplicationItem("LoansToSettle", 1, new Dictionary<string, string>
                    {
                        { "exists", "true" },
                        { "currentDebtAmount", request.LoansToSettleAmount.ToString() },
                        { "shouldBeSettled", "true" },
                        { "loanType", CreditStandardOtherLoanType.Code.unknown.ToString() },
                    }, null);
                }
                else
                {
                    var loanToSettleIndex = 0;
                    foreach (var otherLoan in request.HouseholdOtherLoans)
                    {
                        createRequest.AddComplexApplicationItem("LoansToSettle", loanToSettleIndex++, new Dictionary<string, string>
                        {
                            { "exists", "true" },
                            { "currentDebtAmount", otherLoan.CurrentDebtAmount?.ToString() },
                            { "monthlyCostAmount", otherLoan.MonthlyCostAmount?.ToString() },
                            { "currentInterestRatePercent", otherLoan.CurrentInterestRatePercent?.ToString() },
                            { "shouldBeSettled", ToStringN(otherLoan.ShouldBeSettled, x => x ? "true" : "false") },
                            { "loanType", otherLoan.LoanType },
                        }, null);
                    }
                }
                AddApplicationItem("purposeCode", CreditStandardLoanPurpose.Code.settleOtherLoans.ToString());
            }
            else
                AddApplicationItem("purposeCode", CreditStandardLoanPurpose.Code.newLoan.ToString());

            if (request.RequestedRepaymentTimeInDays.HasValue == request.RequestedRepaymentTimeInMonths.HasValue)
                throw new NTechCoreWebserviceException("Exactly one of RequestedRepaymentTimeInMonths and RequestedRepaymentTimeInDays must be included");
            if(request.RequestedRepaymentTimeInDays.HasValue)
                AddApplicationItem("requestedRepaymentTime", $"{request.RequestedRepaymentTimeInDays.Value}d");
            if (request.RequestedRepaymentTimeInMonths.HasValue)
                AddApplicationItem("requestedRepaymentTime", $"{request.RequestedRepaymentTimeInMonths.Value}m");

            AddApplicationItem("providerApplicationId", request.ProviderApplicationId);
            AddApplicationItem("preScoreResultId", request.PreScoreResultId);
            AddApplicationItem("loanObjective", request.LoanObjective);
            AddApplicationItem("confirmedBankAccountsCode", "Initial");

            var applicantNr = 1;
            void AddApplicantItem(string name, string value)
            {
                if (!applicantItems.ContainsKey(applicantNr))
                    applicantItems[applicantNr] = new Dictionary<string, string>();
                applicantItems[applicantNr][name] = value;
            }

            var civicRegNumberParser = new CivicRegNumberParser(clientConfiguration.Country.BaseCountry);
            foreach (var applicant in request.Applicants)
            {
                var civicRegNr = civicRegNumberParser.Parse(applicant.CivicRegNr);
                var customerId = creationContext.CreateOrUpdatePerson(civicRegNr, new Dictionary<string, string>
                    {
                        { "firstName", applicant.FirstName },
                        { "lastName", applicant.LastName },
                        { "phone", applicant.Phone },
                        { "email", applicant.Email },
                        { "birthDate", applicant.BirthDate?.ToString("yyyy-MM-dd") },
                        { "addressStreet", applicant.AddressStreet },
                        { "addressZipcode", applicant.AddressZipcode },
                        { "addressCity", applicant.AddressCity },
                    },
                    false, createRequest.ApplicationNr, birthDate: applicant.BirthDate);

                createRequest.SetCustomerListMember("Applicant", customerId);

                AddApplicantItem("customerId", customerId.ToString());

                //TODO: Save these to the customer?                
                AddApplicantItem("isOnPepList", ToStringN(applicant?.IsOnPepList, x => x ? "true" : "false"));
                AddApplicantItem("claimsToBePep", ToStringN(applicant?.ClaimsToBePep, x => x ? "true" : "false"));

                AddApplicantItem("marriage", applicant.CivilStatus);
                AddApplicantItem("incomePerMonthAmount", applicant.MonthlyIncomeAmount?.ToString());

                AddApplicantItem("employment", applicant.EmploymentStatus);
                AddApplicantItem("employer", applicant.EmployerName);
                AddApplicantItem("employerPhone", applicant.EmployerPhone);
                AddApplicantItem("employedSince", applicant.EmployedSince?.ToString("yyyy-MM-dd"));
                AddApplicantItem("employedTo", applicant.EmployedTo?.ToString("yyyy-MM-dd"));

                AddApplicantItem("isPartOfTheHousehold", "true"); //Assumption for applicant > 1. Could be changed to not set it at all for applicants > 1
                AddApplicantItem("hasConsentedToShareBankAccountData", applicant.HasConsentedToShareBankAccountData?.ToString()?.ToLower());
                AddApplicantItem("hasConsentedToCreditReport", applicant.HasConsentedToCreditReport?.ToString()?.ToLower());
                AddApplicantItem("claimsToHaveKfmDebt", applicant.ClaimsToHaveKfmDebt?.ToString()?.ToLower());
                AddApplicantItem("hasLegalOrFinancialGuardian", applicant.HasLegalOrFinancialGuardian?.ToString()?.ToLower());
                AddApplicantItem("claimsToBeGuarantor", applicant.ClaimsToBeGuarantor?.ToString()?.ToLower());

                applicantNr++;
            }
            AddApplicationItem("housing", request.HousingType);
            AddApplicationItem("housingCostPerMonthAmount", request.HousingCostPerMonthAmount?.ToString());
            AddApplicationItem("otherHouseholdFixedCostsAmount", request.OtherHouseholdFixedCostsAmount?.ToString());            

            if (request.HouseholdChildren == null && request.NrOfHouseholdChildren.HasValue)
                request.HouseholdChildren = Enumerable.Range(1, request.NrOfHouseholdChildren.Value).Select(x => new UlStandardApplicationRequest.ChildModel
                {
                    Exists = true
                }).ToList();

            AddApplicationItem("childBenefitAmount", request.ChildBenefitAmount?.ToString());
            if (request.HouseholdChildren != null)
            {
                var rowNr = 1;
                foreach (var child in request.HouseholdChildren)
                {
                    createRequest.AddComplexApplicationItem("HouseholdChildren", rowNr, new Dictionary<string, string>
                    {
                        { "exists", "true" },
                        { "ageInYears", child.AgeInYears?.ToString(CultureInfo.InvariantCulture) },
                        { "sharedCustody", child.SharedCustody?.ToString()?.ToLowerInvariant() }
                    }, null);
                    rowNr++;
                }
            }

            createRequest.AddComplexApplicationItem("Application", 1, applicationItems, null);
            foreach (var applicant in applicantItems)
            {
                createRequest.AddComplexApplicationItem("Applicant", applicant.Key, applicant.Value, null);
            }

            createRequest.SetComment("Application created", customerIpAddress: request.Meta.CustomerExternalIpAddress);

            return createRequest;
        }

        private void ValidatDataShare(UlStandardApplicationRequest request, bool isFromInsecureSource)
        {
            if (isFromInsecureSource && request.BankDataShareApplicants?.Count > 0)
                throw new NTechCoreWebserviceException("BankDataShareApplicants forbidden") { IsUserFacing = true, ErrorCode = "bankDataShareForbidden" };

            var hasBankShareData = request.BankDataShareApplicants?.Count > 0;
            if (hasBankShareData && request.Applicants?.Any(x => x.HasDataShare()) == true)
                throw new NTechCoreWebserviceException("BankDataShareApplicants cannot be combined with Applicants DataShare") { IsUserFacing = true };
        }

        private void PopulateDataShare(UlStandardApplicationRequest request, int nrOfApplicants, string applicationNr, Action<int, string, string> setApplicantItem)
        {
            List<UlStandardApplicationBankDataShareApplicantModel> bankDataShareApplicants;
            if (request.BankDataShareApplicants?.Count > 0)
            {
                bankDataShareApplicants = request.BankDataShareApplicants;
            }
            else
            {
                bankDataShareApplicants = new List<UlStandardApplicationBankDataShareApplicantModel>();
                foreach (var applicantNr in Enumerable.Range(1, nrOfApplicants))
                {
                    var applicant = request.Applicants[applicantNr - 1];
                    if (applicant.HasDataShare() && applicant.DataShareProviderName == "kreditz" && applicant.DataShareSessionId != null)
                    {
                        var kreditzData = CreateBankShareApplicantForKreditzCase(applicant.DataShareSessionId, applicant.CivicRegNr, applicantNr, applicationNr);
                        if (kreditzData != null)
                            bankDataShareApplicants.Add(kreditzData);
                    }
                }
            }

            foreach (var applicantNr in Enumerable.Range(1, nrOfApplicants))
            {
                var bankShareData = bankDataShareApplicants.SingleOrDefault(x => x.ApplicantNr == applicantNr);
                if(bankShareData != null)
                {
                    setApplicantItem(applicantNr, "dataShareProviderName", bankShareData.ProviderName);
                    setApplicantItem(applicantNr, "dataShareSessionId", bankShareData.ProviderSessionId);
                    setApplicantItem(applicantNr, "dataShareDate", clock.Now.ToString("yyyy-MM-dd"));
                    if (bankShareData.IncomeAmount.HasValue)
                        setApplicantItem(applicantNr, "dataShareIncomeAmount", bankShareData.IncomeAmount.Value.ToString(CultureInfo.InvariantCulture));
                    if (bankShareData.LtlAmount.HasValue)
                        setApplicantItem(applicantNr, "dataShareLtlAmount", bankShareData.LtlAmount.Value.ToString(CultureInfo.InvariantCulture));
                    if (bankShareData.ProviderDataArchiveKey != null)
                        setApplicantItem(applicantNr, "dataShareArchiveKey", bankShareData.ProviderDataArchiveKey);
                }
            }
        }

        private UlStandardApplicationBankDataShareApplicantModel CreateBankShareApplicantForKreditzCase(string caseId, string civicRegNr, int applicantNr, string applicationNr)
        {
            if (applicantNr != 1)
                return null;

            var settings = KreditzApiClient.GetSettings(environment);

            UlStandardApplicationBankDataShareApplicantModel HandleBankData(ICivicRegNumber dataCivicRegNr, JObject rawBankData)
            {
                var scoringVariables = KreditzApiClient.ParseScoringVariables(rawBankData);
                var archiveKey = documentClient.ArchiveStoreWithSource(Encoding.UTF8.GetBytes(rawBankData.ToString(Newtonsoft.Json.Formatting.None)), "application/json",
                    $"kreditz-data-{caseId}-{applicantNr}.json", $"KreditzApplicantData{applicantNr}", applicationNr);

                return new UlStandardApplicationBankDataShareApplicantModel
                {
                    ApplicantNr = applicantNr,
                    IncomeAmount = scoringVariables.IncomeAmount,
                    LtlAmount = scoringVariables.LtlAmount,
                    ProviderDataArchiveKey = archiveKey,
                    ProviderName = "kreditz",
                    ProviderSessionId = caseId
                };
            }

            var parsedCivicRegNr = new CivicRegNumberParser(clientConfiguration.Country.BaseCountry).Parse(civicRegNr);

            //Handle mock
            if (!envSettings.IsProduction && caseId.StartsWith("kzm_"))
            {
                var mockData = settings.MockDataFile != null
                    ? JObject.Parse(System.IO.File.ReadAllText(settings.MockDataFile))
                    : KreditzApiClient.GenerateTestData(caseId, parsedCivicRegNr, ltlAmount: 100, incomeAmount: 55000);
                return HandleBankData(parsedCivicRegNr, mockData);
            }

            try
            {
                var client = new HttpClient();
                var caseData = syncConverter.ToSync(async () =>
                {
                    var accessToken = await KreditzApiClient.GetCachedAccessTokenAsync(client, settings.ApiClientId, settings.ApiClientSecret, cache.Value);
                    var data = await KreditzApiClient.FindByCase(client, caseId, accessToken, clientConfiguration);
                    if (data.HasData && !settings.SkipDelete)
                    {
                        await KreditzApiClient.DeleteCase(client, caseId, accessToken);
                    }
                    return data;
                });
                if (!caseData.HasData)
                    return null;

                if (parsedCivicRegNr.NormalizedValue != caseData.CivicRegNr.NormalizedValue)
                {
                    if (envSettings.IsProduction || (!envSettings.IsProduction && settings.TestCivicRegNr == null))
                    {
                        //In test we validate unless there is test nr present
                        loggingService.Warning($"Skipped kreditz data on {caseId} since civicRegNrs did not match");
                        return null;
                    }
                }
                return HandleBankData(caseData.CivicRegNr, caseData.RawBankData);
            }
            catch (Exception ex)
            {
                loggingService.Error(ex, $"GetKreditzCaseData: {caseId}");
                return null;
            }
        }

        private string ToStringN<T>(T? value, Func<T, string> transform) where T : struct
        {
            if (!value.HasValue)
                return null;
            return transform(value.Value);
        }
    }
}