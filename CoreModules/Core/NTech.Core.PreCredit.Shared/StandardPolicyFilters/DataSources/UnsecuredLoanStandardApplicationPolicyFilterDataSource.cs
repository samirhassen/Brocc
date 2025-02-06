using nPreCredit.Code.Services;
using nPreCredit.Code.Services.SharedStandard;
using nPreCredit.Code.Services.UnsecuredLoans;
using nPreCredit.Code.StandardPolicyFilters.Rules;
using NTech.Core;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.StandardPolicyFilters.DataSources
{
    public class UnsecuredLoanStandardApplicationPolicyFilterDataSource : StandardApplicationDataSourceBase, IPolicyFilterDataSource
    {
        private readonly bool forceLoadAllVariables;
        private readonly UnsecuredLoanLtlAndDbrService ltlAndDbrService;
        private readonly ICustomerClient customerClient;
        private readonly ILtlDataTables ltlDataTables;
        private Lazy<PreCreditData> preCredit;
        private Lazy<CustomerData> customerData;
        private Dictionary<int, Lazy<ApplicantCreditReportData>> creditReports;
        private Lazy<Dictionary<int, ApplicantCreditData>> creditData;
        private Lazy<Dictionary<int, bool>> hasCheckpointByApplicantNr;

        public UnsecuredLoanStandardApplicationPolicyFilterDataSource(string applicationNr, bool forceLoadAllVariables, bool isAllowedToBuyNewCreditReports,
            UnsecuredLoanLtlAndDbrService ltlAndDbrService, ICoreClock clock, LoanApplicationCreditReportService creditReportService,
            IPreCreditContextFactoryService contextFactoryService, ICustomerClient customerClient, NTech.Core.Module.Shared.Clients.ICreditClient creditClient,
            NTech.Core.Module.Shared.Clients.ICreditReportClient creditReportClient, IClientConfigurationCore clientConfiguration, ILtlDataTables ltlDataTables) : base(contextFactoryService, customerClient, creditClient,
                creditReportClient, clientConfiguration, creditReportService, clock)
        {
            this.forceLoadAllVariables = forceLoadAllVariables;
            this.ltlAndDbrService = ltlAndDbrService;
            this.customerClient = customerClient;
            this.ltlDataTables = ltlDataTables;
            preCredit = new Lazy<PreCreditData>(() => LoadPreCreditData(applicationNr));
            customerData = new Lazy<CustomerData>(() => LoadCustomerData(preCredit.Value.CustomerIdByApplicantNr));

            ApplicantCreditReportData LoadCreditReportForApplicantLocal(int applicantNr)
            {
                var customerId = preCredit.Value.CustomerIdByApplicantNr[applicantNr];
                var civicRegNr = customerData.Value.CivicRegNrByApplicantNr[applicantNr];
                return LoadCreditReportForApplicant(customerId, applicationNr, isAllowedToBuyNewCreditReports, civicRegNr, applicantNr);
            }

            creditReports = new Dictionary<int, Lazy<ApplicantCreditReportData>>
            {
                { 1, new Lazy<ApplicantCreditReportData>(() => LoadCreditReportForApplicantLocal(1)) },
                { 2, new Lazy<ApplicantCreditReportData>(() => LoadCreditReportForApplicantLocal(2)) }
            };

            creditData = new Lazy<Dictionary<int, ApplicantCreditData>>(() => LoadCreditData(preCredit.Value.CustomerIdByApplicantNr));
            hasCheckpointByApplicantNr = new Lazy<Dictionary<int, bool>>(() => LoadCustomerHasCheckpoint());
        }

        public VariableSet LoadVariables(ISet<string> applicationVariableNames, ISet<string> applicantVariableNames)
        {
            var result = new VariableSet(preCredit.Value.NrOfApplicants);
            var applicantNrs = Enumerable.Range(1, preCredit.Value.NrOfApplicants).ToList();
            var v = new VariableSetHelper(applicationVariableNames, applicantVariableNames, applicantNrs, forceLoadAllVariables, result);

            v.HandleApplicationIntVariable("leftToLiveOnAmount", GetLeftToLiveOn);
            v.HandleApplicationIntVariable("dataShareLeftToLiveOnAmount", () => preCredit.Value.ApplicantList.GetRow(1, true).GetUniqueItemInteger("dataShareLtlAmount") ?? 0);
            v.HandleApplicationDecimalVariable("debtBurdenRatio", GetDbr);
            HandleWeighedAverageLoansToSettleVariables(v);
            v.HandleApplicantIntVariable("applicantAgeInYears", x => customerData.Value.AgeInYearsByApplicantNr.OptSDefaultValue(x));
            v.HandleApplicantBoolVariable("applicantHasAddress", x => customerData.Value.ZipCodeByApplicantNr.Opt(x) != null);
            v.HandleApplicantStringVariable("applicantEmploymentFormCode", x => preCredit.Value.ApplicantList.GetRow(x, true).GetUniqueItem("employment"));
            v.HandleApplicantBoolVariable("isApplicantMissingCreditReportConsent", x => preCredit.Value.ApplicantList.GetRow(x, true).GetUniqueItemBoolean("hasConsentedToCreditReport") != true);
            v.HandleApplicantBoolVariable("applicantClaimsLegalOrFinancialGuardian", x => preCredit.Value.ApplicantList.GetRow(x, true).GetUniqueItemBoolean("hasLegalOrFinancialGuardian") == true);
            v.HandleApplicantBoolVariable("applicantClaimsToBeGuarantor", x => preCredit.Value.ApplicantList.GetRow(x, true).GetUniqueItemBoolean("claimsToBeGuarantor") == true);
            v.HandleApplicantIntVariable("applicantIncomePerMonth", x => preCredit.Value.ApplicantList.GetRow(x, true).GetUniqueItemInteger("incomePerMonthAmount") ?? 0);
            v.HandleApplicantIntVariable("applicantDataShareIncomePerMonth", x => preCredit.Value.ApplicantList.GetRow(x, true).GetUniqueItemInteger("dataShareIncomeAmount") ?? 0);
            v.HandleApplicantBoolVariable("applicantHasOtherActiveApplicationsInSystem", x => preCredit.Value.HasOtherActiveApplicationsByApplicantNr[x]);
            v.HandleApplicationIntVariable("mainApplicantCreditReportNrOfPaymentRemarks", () => creditReports[1].Value.NrOfPaymentRemarks);
            v.HandleCoApplicantOnly("coApplicantCreditReportNrOfPaymentRemarks", x => v.HandleApplicationIntVariable(x, () => creditReports[2].Value.NrOfPaymentRemarks));
            v.HandleApplicationDecimalVariable("mainApplicantCreditReportRiskValue", () => creditReports[1].Value.RiskValue);
            v.HandleCoApplicantOnly("coApplicantCreditReportRiskValue", x => v.HandleApplicationDecimalVariable(x, () => creditReports[2].Value.RiskValue));
            v.HandleApplicantBoolVariable("applicantHasBoxAddress", x => creditReports[x].Value.HasPostBoxAddress);
            v.HandleApplicantBoolVariable("applicantHasPosteRestanteAddress", x => creditReports[x].Value.HasPosteRestanteAddress);
            v.HandleApplicantIntVariable("applicantCreditReportIncomePerMonth", x =>
            {
                var latestIncomePerYear = creditReports[x].Value.LatestIncomePerYear;
                return latestIncomePerYear.HasValue
                    ? (int)Math.Ceiling(((decimal)latestIncomePerYear.Value) / 12m)
                    : new int?();
            });

            v.HandleApplicantBoolVariable("applicantCreditReportHasKfmBalance", x => creditReports[x].Value.HasKfmBalance);
            v.HandleApplicantBoolVariable("applicantCreditReportHasSESkuldsanering", x => creditReports[x].Value.HasSwedishSkuldsanering);
            v.HandleApplicationDecimalVariable("mainApplicantCreditReportScoreValue", () => creditReports[1].Value.ScoreValue);
            v.HandleApplicantBoolVariable("applicantCreditReportHasDomesticAddress", x => creditReports[x].Value.HasDomesticAddress);
            v.HandleApplicantBoolVariable("applicantHasGuardian", x => creditReports[x].Value.HasGuardian);
            v.HandleApplicantStringVariable("applicantStatusCode", x => creditReports[x].Value.ApplicantStatusCode);
            v.HandleApplicantBoolVariable("applicantHasLoanInSystem", x => creditData.Value[x].HasActiveLoanInSystem);
            v.HandleApplicationStringVariable("loanObjective", () => preCredit.Value.ApplicationList.GetRow(1, true).GetUniqueItem("loanObjective"));
            v.HandleApplicantBoolVariable("applicantHasActiveCheckpoint", x => hasCheckpointByApplicantNr.Value[x]);
            
            return result;
        }

        private void HandleWeighedAverageLoansToSettleVariables(VariableSetHelper v)
        {
            var values = new Lazy<(decimal? WeightedAverageSettlementInterestRatePercent, decimal? MinSettlementInterestRatePercent, bool HasLoansToSettle)>(() =>
                MinAllowedWeightedAverageSettlementInterestRateRule.ComputeWeightedSettlementInterestRateScoringVariables(preCredit.Value.LoansToSettleList));

            v.HandleApplicationDecimalVariable("weightedAverageSettlementInterestRatePercent", () => values.Value.WeightedAverageSettlementInterestRatePercent);
            v.HandleApplicationDecimalVariable("minSettlementInterestRatePercent", () => values.Value.MinSettlementInterestRatePercent);
            v.HandleApplicationBoolVariable("hasLoansToSettle", () => values.Value.HasLoansToSettle);
        }

        private PreCreditData LoadPreCreditData(string applicationNr)
        {
            using (var context = contextFactoryService.CreateExtended())
            {
                var application = context
                    .CreditApplicationHeadersQueryable
                    .Where(x => x.ApplicationNr == applicationNr)
                    .Select(x => new
                    {
                        x.NrOfApplicants,
                        x.ComplexApplicationListItems
                    })
                    .SingleOrDefault();
                if (application == null)
                    throw new Exception("No such application exists: " + applicationNr);

                var applicationList = ComplexApplicationList.CreateListFromFlattenedItems("Application", application.ComplexApplicationListItems);
                var applicantList = ComplexApplicationList.CreateListFromFlattenedItems("Applicant", application.ComplexApplicationListItems);
                var householdChildrenList = ComplexApplicationList.CreateListFromFlattenedItems("HouseholdChildren", application.ComplexApplicationListItems);
                var loansToSettleList = ComplexApplicationList.CreateListFromFlattenedItems("LoansToSettle", application.ComplexApplicationListItems);
                var customerIdByApplicantNr = Enumerable
                        .Range(1, application.NrOfApplicants)
                        .Select(x => new
                        {
                            applicantNr = x,
                            customerId = applicantList
                                .GetRow(x, true)
                                .GetUniqueItemInteger("customerId", require: true).Value
                        })
                        .ToDictionary(x => x.applicantNr, x => x.customerId);

                var customerIds = customerIdByApplicantNr.Select(x => x.Value).ToList();
                var customerIdsWithExistingApplications = context.CreditApplicationCustomerListMembersQueryable
                    .Where(clm => customerIds.Contains(clm.CustomerId)
                            && clm.CreditApplication.IsActive
                            && clm.ApplicationNr != applicationNr
                            && clm.ListName == "Applicant")
                    .Select(x => x.CustomerId)
                    .ToList();

                var hasActiveAppsByApplicantNrs = customerIdByApplicantNr
                    .Select(x => new { applicantNr = x.Key, hasActiveApplication = customerIdsWithExistingApplications.Contains(x.Value) })
                    .ToDictionary(x => x.applicantNr, x => x.hasActiveApplication);

                return new PreCreditData
                {
                    NrOfApplicants = application.NrOfApplicants,
                    ApplicationList = applicationList,
                    ApplicantList = applicantList,
                    HouseholdChildrenList = householdChildrenList,
                    LoansToSettleList = loansToSettleList,
                    CustomerIdByApplicantNr = customerIdByApplicantNr,
                    HasOtherActiveApplicationsByApplicantNr = hasActiveAppsByApplicantNrs
                };
            }
        }

        private Dictionary<int, bool> LoadCustomerHasCheckpoint()
        {
            var applicants = preCredit.Value.CustomerIdByApplicantNr.Select(x => new { ApplicantNr = x.Key, CustomerId = x.Value }).ToList();
            var customerIds = applicants.Select(x => x.CustomerId).ToHashSetShared();
            var result = customerClient.GetActiveCheckpointIdsOnCustomerIds(customerIds, null);
            return applicants.ToDictionary(x => x.ApplicantNr, x => result.CheckPointByCustomerId.ContainsKey(x.CustomerId));

        }

        private int? GetLeftToLiveOn()
        {
            var d = preCredit.Value;

            var ltlService = new LoanStandardLtlService(ltlDataTables);

            var ltl = ltlService.CalculateLeftToLiveOnForUnsecuredLoan(d.ApplicationList, d.ApplicantList, customerData.Value.AgeInYearsByApplicantNr, d.HouseholdChildrenList, d.LoansToSettleList);
            return ltl.LtlAmount;
        }

        private decimal? GetDbr()
        {
            var d = preCredit.Value;

            return ltlAndDbrService.CalculateDbr(d.ApplicationList, d.ApplicantList, d.LoansToSettleList, null);
        }

        private class PreCreditData
        {
            public int NrOfApplicants { get; set; }
            public Dictionary<int, int> CustomerIdByApplicantNr { get; set; }
            public Dictionary<int, bool> HasOtherActiveApplicationsByApplicantNr { get; set; }
            public ComplexApplicationList ApplicationList { get; set; }
            public ComplexApplicationList ApplicantList { get; set; }
            public ComplexApplicationList HouseholdChildrenList { get; set; }
            public ComplexApplicationList LoansToSettleList { get; set; }
        }
    }
}