using nPreCredit.Code.Services;
using nPreCredit.Code.Services.SharedStandard;
using NTech.Core;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.StandardPolicyFilters.DataSources
{
    public class MortgageLoanStandardApplicationPolicyFilterDataSource : StandardApplicationDataSourceBase, IPolicyFilterDataSource
    {
        private readonly bool forceLoadAllVariables;
        private Lazy<PreCreditData> preCredit;
        private Lazy<CustomerData> customerData;
        private Dictionary<int, Lazy<ApplicantCreditReportData>> creditReports;
        private Lazy<Dictionary<int, ApplicantCreditData>> creditData;

        public MortgageLoanStandardApplicationPolicyFilterDataSource(string applicationNr, bool forceLoadAllVariables, bool isAllowedToBuyNewCreditReports, ICoreClock clock, IPreCreditContextFactoryService contextFactoryService,
            LoanApplicationCreditReportService creditReportService, ICustomerClient customerClient, NTech.Core.Module.Shared.Clients.ICreditClient creditClient,
            NTech.Core.Module.Shared.Clients.ICreditReportClient creditReportClient, IClientConfigurationCore clientConfiguration) :
            base(contextFactoryService, customerClient, creditClient, creditReportClient, clientConfiguration, creditReportService, clock)
        {
            this.forceLoadAllVariables = forceLoadAllVariables;
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
        }

        public VariableSet LoadVariables(ISet<string> applicationVariableNames, ISet<string> applicantVariableNames)
        {
            var result = new VariableSet(preCredit.Value.NrOfApplicants);
            var applicantNrs = Enumerable.Range(1, preCredit.Value.NrOfApplicants).ToList();
            var v = new VariableSetHelper(applicationVariableNames, applicantVariableNames, applicantNrs, forceLoadAllVariables, result);

            v.HandleApplicationIntVariable("leftToLiveOnAmount", GetLeftToLiveOn);
            v.HandleApplicationDecimalVariable("loanToValuePercent", GetLoanToValuePercent);
            v.HandleApplicationDecimalVariable("loanToIncome", GetLoanToIncome);
            v.HandleApplicationStringVariable("objectZipCode", () => preCredit.Value.ApplicationList.GetRow(1, true).GetUniqueItem("objectAddressZipcode"));

            v.HandleApplicantIntVariable("applicantAgeInYears", x => customerData.Value.AgeInYearsByApplicantNr.OptSDefaultValue(x));
            v.HandleApplicantBoolVariable("applicantHasAddress", x => customerData.Value.ZipCodeByApplicantNr.Opt(x) != null);

            v.HandleApplicantStringVariable("applicantEmploymentFormCode", x => preCredit.Value.ApplicantList.GetRow(x, true).GetUniqueItem("employment"));
            v.HandleApplicantBoolVariable("isApplicantMissingCreditReportConsent", x => preCredit.Value.ApplicantList.GetRow(x, true).GetUniqueItemBoolean("hasConsentedToCreditReport") != true);
            v.HandleApplicantIntVariable("applicantIncomePerMonth", x => preCredit.Value.ApplicantList.GetRow(x, true).GetUniqueItemInteger("incomePerMonthAmount") ?? 0);
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
            v.HandleApplicantBoolVariable("applicantHasGuardian", x => creditReports[x].Value.HasGuardian);
            v.HandleApplicantStringVariable("applicantStatusCode", x => creditReports[x].Value.ApplicantStatusCode);

            v.HandleApplicantBoolVariable("applicantHasLoanInSystem", x => creditData.Value[x].HasActiveLoanInSystem);

            return result;
        }

        private static PreCreditData LoadPreCreditData(string applicationNr)
        {
            using (var context = new PreCreditContext())
            {
                var application = context
                    .CreditApplicationHeaders
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
                var mortgageLoansToSettleList = ComplexApplicationList.CreateListFromFlattenedItems("MortgageLoansToSettle", application.ComplexApplicationListItems);
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
                var customerIdsWithExistingApplications = context.CreditApplicationCustomerListMembers
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
                    MortgageLoansToSettle = mortgageLoansToSettleList,
                    CustomerIdByApplicantNr = customerIdByApplicantNr,
                    HasOtherActiveApplicationsByApplicantNr = hasActiveAppsByApplicantNrs
                };
            }
        }

        private int? GetLeftToLiveOn()
        {
            var d = preCredit.Value;

            var ltlService = new LoanStandardLtlService(new LtlDataTables());

            var ltl = ltlService.CalculateLeftToLiveOnForMortgageLoan(
                d.ApplicationList,
                d.ApplicantList,
                customerData.Value.AgeInYearsByApplicantNr,
                d.HouseholdChildrenList,
                d.LoansToSettleList, d.MortgageLoansToSettle);
            return ltl.LtlAmount;
        }

        private decimal? GetLoanToValuePercent()
        {
            var d = preCredit.Value;
            return MortgageLoanLtxService.CalculateLoanToValue(d.ApplicationList, d.MortgageLoansToSettle, null) * 100m;
        }

        private decimal? GetLoanToIncome()
        {
            var d = preCredit.Value;
            return MortgageLoanLtxService.CalculateLoanToIncome(d.ApplicationList, d.ApplicantList, d.MortgageLoansToSettle, d.LoansToSettleList, null);
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
            public ComplexApplicationList MortgageLoansToSettle { get; set; }
        }
    }
}