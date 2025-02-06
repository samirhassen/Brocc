using nPreCredit.Code.Datasources;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.CompanyLoans;
using NTech;
using NTech.Banking.OrganisationNumbers;
using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.BalanziaSe
{
    public class BalanziaSeScoringProcessDataSource : IPluginScoringProcessDataSource
    {
        private readonly ApplicationDataSourceService ds;
        private readonly ICreditClient creditClient;
        private readonly ICustomerServiceRepository customerServiceRepository;
        private readonly IClock clock;
        private readonly ICompanyLoanCustomerCardUpdateService companyLoanCustomerCardUpdateService;
        private readonly IReferenceInterestRateService referenceInterestRateService;
        private readonly CreditReportService creditReportService;

        public BalanziaSeScoringProcessDataSource(ApplicationDataSourceService ds, ICreditClient creditClient, ICustomerServiceRepository customerServiceRepository, IClock clock,
            ICompanyLoanCustomerCardUpdateService companyLoanCustomerCardUpdateService, IReferenceInterestRateService referenceInterestRateService,
            CreditReportService creditReportService)
        {
            this.ds = ds;
            this.creditClient = creditClient;
            this.customerServiceRepository = customerServiceRepository;
            this.clock = clock;
            this.companyLoanCustomerCardUpdateService = companyLoanCustomerCardUpdateService;
            this.referenceInterestRateService = referenceInterestRateService;
            this.creditReportService = creditReportService;
        }

        public ScoringDataModel GetItems(string objectId, ISet<string> applicationItems, ISet<string> applicantItems)
        {
            return GetItemsWithInternalHistory(objectId, applicationItems, applicantItems)?.ScoringData;
        }

        private void AppendCompanyCustomerCreditReportData(
            string objectId,
            ScoringDataModel model,
            ISet<string> scoringVariables,
            IOrganisationNumber companyOrgnr,
            int companyCustomerId,
            string applicantEmail)
        {
            var reportResult = creditReportService.BuyCompanyCreditReport(companyOrgnr, companyCustomerId, new List<string> { "*" }, "UcBusinessSe", false, true, (type: "CreditApplication", data: objectId), new Dictionary<string, string>());
            if (reportResult.IsInvalidCredentialsError)
                throw new ServiceException("Credit report provider reported invalid credentials") { IsUserSafeException = true, ErrorCode = "creditReportProviderInvalidCredentials" };
            else if (reportResult.ProviderIsDown)
                throw new ServiceException("Credit report did not respond/might be down") { IsUserSafeException = true, ErrorCode = "creditReportProviderIsDown" };
            else if (reportResult.Model == null)
                throw new ServiceException($"Credit report request failed: {reportResult.ErrorMessage}") { IsUserSafeException = true, ErrorCode = "creditReportProviderError" };

            foreach (var s in scoringVariables)
            {
                var name = s.Substring("creditReport".Length);
                name = name.Substring(0, 1).ToLowerInvariant() + name.Substring(1);
                model.Set(s, reportResult.Model.Get(name).StringValue.Optional, null);
            }

            if (!scoringVariables.Contains("companyCreditReportHtmlArchiveKey")) //Always include this
                model.Set("companyCreditReportHtmlArchiveKey", reportResult.Model.Get("htmlReportArchiveKey").StringValue.Optional, null);
            if (!scoringVariables.Contains("snikod"))
                model.Set("companyCreditReportSnikod", reportResult.Model.Get("snikod").StringValue.Optional, null);

            UpdateCustomerCard(objectId, companyOrgnr, companyCustomerId, reportResult, applicantEmail);
        }

        private void UpdateCustomerCard(string objectId, IOrganisationNumber companyOrgnr, int companyCustomerId, CreditReportService.PartialCreditReportModelAndStatus reportResult, string applicantEmail)
        {
            if (!reportResult.IsNewReport)
                return;

            var customerData = new Dictionary<string, string>();
            Action<string, string> addIfPresent = (creditReportName, customerName) =>
            {
                var v = reportResult.Model.Get(creditReportName).StringValue.Optional;
                if (!string.IsNullOrWhiteSpace(v))
                    customerData[customerName] = v;
            };
            addIfPresent("companyName", "companyName");
            addIfPresent("addressStreet", "addressStreet");
            addIfPresent("addressZipcode", "addressZipcode");
            addIfPresent("addressCity", "addressCity");
            addIfPresent("phone", "phone");
            addIfPresent("snikod", "snikod");
            if (!string.IsNullOrWhiteSpace(applicantEmail))
                customerData["email"] = applicantEmail;

            this.companyLoanCustomerCardUpdateService.CreateOrUpdateCompany(
                companyOrgnr, customerData, true,
                CompanyLoanCustomerRoleCode.CustomerCompany, CompanyLoanCustomerCardUpdateEventCode.NewCreditReport,
                objectId, expectedCustomerId: companyCustomerId);
        }

        private void AppendCustomerServiceData(
            string objectId,
            ScoringDataModel model,
            ISet<string> scoringVariables,
            ISet<int> customerIds,
            out List<HistoricalApplication> historicalApplications)
        {
            var otherApplications = GetOtherApplications(objectId, customerIds);
            historicalApplications = otherApplications;

            var pausedUntilDate = otherApplications
                ?.Where(x => x.PauseItems != null)
                ?.SelectMany(x => x?.PauseItems?.Select(y => new { x.ApplicationNr, y.PausedUntilDate }))
                ?.Where(x => x.PausedUntilDate >= clock.Today)
                ?.Max(x => (DateTime?)x.PausedUntilDate);

            Action<string, Action<string>> add = (name, set) =>
            {
                if (scoringVariables.Contains(name))
                    set(name);
            };

            var activeOtherApplications = otherApplications.Where(x => !x.IsFinalDecisionMade && x.IsActive);
            var maxActiveApplicationDate = otherApplications.Where(x => !x.IsFinalDecisionMade && x.IsActive).Max(x => (DateTime?)x.ApplicationDate.Date.Date);
            var minActiveApplicationDate = otherApplications.Where(x => !x.IsFinalDecisionMade && x.IsActive).Min(x => (DateTime?)x.ApplicationDate.Date.Date);

            var activeApplicationCount = activeOtherApplications.Count();
            add("pausedDays",
                x => model.Set(x, pausedUntilDate.HasValue ? ((int)Math.Round(pausedUntilDate.Value.Subtract(clock.Today.Date).TotalDays)) : 0, null));
            add("activeApplicationCount",
                x => model.Set(x, activeApplicationCount, null));
            add("maxActiveApplicationAgeInDays",
                x => model.Set(x, maxActiveApplicationDate.HasValue ? ((int)Math.Round(clock.Today.Date.Subtract(maxActiveApplicationDate.Value).TotalDays)) : 0, null));
            add("minActiveApplicationAgeInDays",
                x => model.Set(x, minActiveApplicationDate.HasValue ? ((int)Math.Round(clock.Today.Date.Subtract(minActiveApplicationDate.Value).TotalDays)) : 0, null));
        }

        private void AppendInternalCreditHistoryData(
            ScoringDataModel m,
            ISet<string> creditHistoryScoringVariables,
            ISet<int> customerIds,
            out List<HistoricalCredit> historicalCredits)
        {
            var localHistoricalCredits = GetLoans(customerIds);
            historicalCredits = localHistoricalCredits;


            Action<string, Action<string>> add = (name, set) =>
            {
                if (creditHistoryScoringVariables.Contains(name))
                    set(name);
            };

            add("nrOfActiveLoans", x => m.Set(x, localHistoricalCredits.Where(y => y.Status == "Normal").Count(), null));
            var maxNrOfDaysBetweenDueDateAndPaymentEver = localHistoricalCredits.Max(x => x.MaxNrOfDaysBetweenDueDateAndPaymentEver) ?? 0;
            add("maxNrOfDaysBetweenDueDateAndPaymentEver", x => m.Set(x, maxNrOfDaysBetweenDueDateAndPaymentEver, null));
            add("historicalDebtCollectionCount", x => m.Set(x, localHistoricalCredits.Where(y => y.IsOrHasBeenOnDebtCollection).Count(), null));
        }

        private List<HistoricalApplication> GetOtherApplications(string applicationNr, ISet<int> customerIds)
        {
            return this
                    .customerServiceRepository
                    .FindByCustomerIds(customerIds.ToArray())
                    .SelectMany(x => x.Value)
                    .GroupBy(x => x.ApplicationNr)
                    .Select(x => x.First())
                    .Where(x => x.ApplicationNr != applicationNr)
                    .ToList();
        }

        private List<HistoricalCredit> GetLoans(ISet<int> customerIds)
        {
            return creditClient.GetCustomerCreditHistory(customerIds.ToList());
        }

        private ApplicationDataSourceServiceRequest CreateApplicationDataSourceServiceRequest()
        {
            return new ApplicationDataSourceServiceRequest
            {
                DataSourceName = CreditApplicationItemDataSource.DataSourceNameShared,
                MissingItemStrategy = ApplicationDataSourceMissingItemStrategy.UseDefaultValue,
                GetDefaultValue = x => "missing",
                Names = new HashSet<string>()
            };
        }

        private void RequireApplicationCustomerIds(ApplicationDataSourceServiceRequest request)
        {
            if (request.Names == null)
                request.Names = new HashSet<string>();

            request.Names.Add("application.applicantCustomerId");
            request.Names.Add("application.companyCustomerId");
        }

        private ISet<int> GetCustomerIdsFromApplicationDataSourceResult(Dictionary<string, Dictionary<string, string>> result)
        {
            var companyCustomerId = int.Parse(result[CreditApplicationItemDataSource.DataSourceNameShared]["application.companyCustomerId"]);
            var applicantCustomerId = int.Parse(result[CreditApplicationItemDataSource.DataSourceNameShared]["application.applicantCustomerId"]);

            return new List<int>() { companyCustomerId, applicantCustomerId }.ToHashSet();
        }

        public PluginScoringProcessModelWithInternalHistory GetItemsWithInternalHistory(string objectId, ISet<string> applicationItems, ISet<string> applicantItems)
        {
            var model = new ScoringDataModel();

            //Computed
            if (applicationItems.Contains("nrOfMonthsLeftCurrentYear"))
            {
                model.Set("nrOfMonthsLeftCurrentYear", NTech.Dates.GetAbsoluteNrOfMonthsBetweenDates(this.clock.Today, new DateTime(this.clock.Today.Year, 12, 31)), null);
            }

            if (applicationItems.Contains("currentReferenceInterestRatePercent"))
            {
                model.Set("currentReferenceInterestRatePercent", referenceInterestRateService.GetCurrent(), null);
            }

            //Application
            var requests = new List<ApplicationDataSourceServiceRequest>();
            var applicationDataRequest = CreateApplicationDataSourceServiceRequest();
            requests.Add(applicationDataRequest);

            //Needed for credit history
            this.RequireApplicationCustomerIds(applicationDataRequest);
            applicationDataRequest.Names.Add("application.companyOrgnr");

            var populators = new List<Action<ScoringDataModel, Dictionary<string, Dictionary<string, string>>>>();

            void AddApplicationItem(string scoringVariableName, string applicationDataSourceName)
            {
                if (applicationItems.Contains(scoringVariableName))
                {
                    applicationDataRequest.Names.Add(applicationDataSourceName);
                    populators.Add((m, r) => m.Set(scoringVariableName, r[CreditApplicationItemDataSource.DataSourceNameShared].Opt(applicationDataSourceName), null));
                }
            }

            AddApplicationItem("applicationCompanyAgeInMonths", "application.companyAgeInMonths");
            AddApplicationItem("applicationCompanyYearlyRevenue", "application.companyYearlyRevenue");
            AddApplicationItem("applicationAmount", "application.amount");
            AddApplicationItem("applicationRepaymentTimeInMonths", "application.repaymentTimeInMonths");
            AddApplicationItem("applicationCompanyYearlyResult", "application.companyYearlyResult");
            AddApplicationItem("applicationCompanyCurrentDebtAmount", "application.companyCurrentDebtAmount");
            AddApplicationItem("applicationLoanPurposeCode", "application.loanPurposeCode");

            var requestsArr = requests.Where(x => x.Names.Count > 0).ToArray();

            var result = ds.GetData(objectId, requestsArr);
            foreach (var p in populators)
                p(model, result);

            var companyCustomerId = int.Parse(result[CreditApplicationItemDataSource.DataSourceNameShared]["application.companyCustomerId"]);
            var companyOrgnr = NEnv.BaseOrganisationNumberParser.Parse(result[CreditApplicationItemDataSource.DataSourceNameShared]["application.companyOrgnr"]);

            var customerIds = GetCustomerIdsFromApplicationDataSourceResult(result);

            var creditHistoryScoringVariables = new List<string>();

            void AddCreditHistoryItem(string scoringVariableName)
            {
                if (applicationItems.Contains(scoringVariableName)) creditHistoryScoringVariables.Add(scoringVariableName);
            }

            AddCreditHistoryItem("nrOfActiveLoans");
            AddCreditHistoryItem("maxNrOfDaysBetweenDueDateAndPaymentEver");
            AddCreditHistoryItem("historicalDebtCollectionCount");

            AppendInternalCreditHistoryData(
                model,
                creditHistoryScoringVariables.ToHashSet(),
                customerIds.ToHashSet(),
                out var historicalCredits);

            //Customer service applications
            var customerServiceScoringVariables = new HashSet<string>();

            void AddCustomerServiceItem(string scoringVariableName)
            {
                if (applicationItems.Contains(scoringVariableName)) customerServiceScoringVariables.Add(scoringVariableName);
            }

            AddCustomerServiceItem("pausedDays");
            AddCustomerServiceItem("activeApplicationCount");
            AddCustomerServiceItem("maxActiveApplicationAgeInDays");
            AddCustomerServiceItem("minActiveApplicationAgeInDays");

            AppendCustomerServiceData(objectId, model, customerServiceScoringVariables, customerIds.ToHashSet(), out var historicalApplications);

            //Credit reports
            var creditReportScoringVariables = new HashSet<string>();
            foreach (var i in applicationItems.Where(x => x.StartsWith("creditReport")))
            {
                creditReportScoringVariables.Add(i);
            }

            var forceExternalScoring = GetForceExternalScoring(objectId);

            if (creditReportScoringVariables.Any() || forceExternalScoring)
                AppendCompanyCustomerCreditReportData(objectId, model, creditReportScoringVariables, companyOrgnr, companyCustomerId, GetApplicantEmail(objectId));

            return new PluginScoringProcessModelWithInternalHistory
            {
                ScoringData = model,
                HistoricalApplications = historicalApplications,
                HistoricalCredits = historicalCredits
            };
        }

        // From CreditApplicationItem
        private string GetApplicantEmail(string objectId)
        {
            var r = CreateApplicationDataSourceServiceRequest();
            r.Names.Add("application.applicantEmail");
            return ds.GetData(objectId, r).Single().Value.Opt("application.applicantEmail");
        }

        // From CreditApplicationItem
        private bool GetForceExternalScoring(string objectId)
        {
            var request = CreateApplicationDataSourceServiceRequest();
            request.Names.Add("application.forceExternalScoring");
            return ds.GetData(objectId, request).Single().Value.Opt("application.forceExternalScoring") == "Yes";
        }
    }
}