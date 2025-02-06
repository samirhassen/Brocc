using nPreCredit.Code.Services;
using nPreCredit.Code.Services.SharedStandard;
using NTech.Banking.CivicRegNumbers;
using NTech.Core;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.StandardPolicyFilters.DataSources
{
    public abstract class StandardApplicationDataSourceBase
    {
        protected readonly IPreCreditContextFactoryService contextFactoryService;
        private readonly ICustomerClient customerClient;
        private readonly ICreditClient creditClient;
        private readonly ICreditReportClient creditReportClient;
        private readonly LoanApplicationCreditReportService loanApplicationCreditReportService;
        private readonly ICoreClock clock;
        private readonly Lazy<CivicRegNumberParser> civicRegNumberParser;

        public StandardApplicationDataSourceBase(IPreCreditContextFactoryService contextFactoryService, ICustomerClient customerClient, ICreditClient creditClient, ICreditReportClient creditReportClient,
            IClientConfigurationCore clientConfiguration, LoanApplicationCreditReportService loanApplicationCreditReportService, ICoreClock clock)
        {
            this.contextFactoryService = contextFactoryService;
            this.customerClient = customerClient;
            this.creditClient = creditClient;
            this.creditReportClient = creditReportClient;
            this.loanApplicationCreditReportService = loanApplicationCreditReportService;
            this.clock = clock;
            this.civicRegNumberParser = new Lazy<CivicRegNumberParser>(() => new CivicRegNumberParser(clientConfiguration.Country.BaseCountry));
        }

        protected ApplicantCreditReportData LoadCreditReportForApplicant(
            int customerId, string applicationNr,
            bool isAllowedToBuyNewCreditReports, ICivicRegNumber civicRegNr, int applicantNr)
        {
            try
            {
                var result = loanApplicationCreditReportService.FindExistingOrBuyNewCreditReport(customerId, applicationNr, isAllowedToBuyNewCreditReports, civicRegNr, creditReportClient, true, clock);
                var requestedFields = result.RequestedItems;
                var creditReportItems = result.Report;
                StringItem GetRequestedItem(string name)
                {
                    if (!requestedFields.Contains(name))
                        throw new Exception($"You forgot to request {name}");
                    return creditReportItems.Get(name);
                }

                return new ApplicantCreditReportData
                {
                    NrOfPaymentRemarks = GetRequestedItem("nrOfPaymentRemarks").IntValue.Optional,
                    RiskValue = GetRequestedItem("riskValue").DecimalValue.Optional,
                    HasPostBoxAddress = GetRequestedItem("hasPostBoxAddress").BoolValue.Optional,
                    HasPosteRestanteAddress = GetRequestedItem("hasPosteRestanteAddress").BoolValue.Optional,
                    LatestIncomePerYear = GetRequestedItem("latestIncomePerYear").IntValue.Optional,
                    HasGuardian = GetRequestedItem("hasGuardian").BoolValue.Optional,
                    ApplicantStatusCode = GetRequestedItem("personstatus").StringValue.Optional,
                    HasKfmBalance = GetRequestedItem("hasKfmBalance").BoolValue.Optional,
                    HasSwedishSkuldsanering = GetRequestedItem("hasSwedishSkuldsanering").BoolValue.Optional,
                    ScoreValue = GetRequestedItem("scoreValue").DecimalValue.Optional,
                    HasDomesticAddress = GetRequestedItem("hasDomesticAddress").BoolValue.Optional
                };
            }
            catch(NTechCoreWebserviceException ex)
            {
                if (ex.ErrorCode == "failedToBuyCreditReport")
                {
                    using (var context = contextFactoryService.CreateExtended())
                    {
                        context.AddCreditApplicationComments(context.FillInfrastructureFields(new CreditApplicationComment
                        {
                            ApplicationNr = applicationNr,
                            CommentText = $"Failed to buy creditreport for applicant {applicantNr} during scoring: {ex.Message}. Rules using credit report data will be skipped.",
                            EventType = "FailedCreditReportDuringScoring",
                            CommentById = context.CurrentUserId,
                            CommentDate = context.CoreClock.Now
                        }));
                        context.SaveChanges();
                    }

                    return new ApplicantCreditReportData();
                }
                else
                    throw;
            }
        }

        protected class ApplicantCreditReportData
        {
            public int? NrOfPaymentRemarks { get; set; }
            public decimal? RiskValue { get; set; }
            public bool? HasPostBoxAddress { get; set; }
            public bool? HasPosteRestanteAddress { get; set; }
            public int? LatestIncomePerYear { get; set; }
            public bool? HasGuardian { get; set; }
            public string ApplicantStatusCode { get; set; }
            public bool? HasKfmBalance { get; set; }
            public bool? HasSwedishSkuldsanering { get; set; }
            public decimal? ScoreValue { get; set; }
            public bool? HasDomesticAddress { get; set; }
        }

        protected CustomerData LoadCustomerData(Dictionary<int, int> customerIdByApplicantNr)
        {
            var customerProperties = customerClient.BulkFetchPropertiesByCustomerIdsD(customerIdByApplicantNr.Values.ToHashSetShared(), "civicRegNr", "addressZipcode");

            var civicRegNrByApplicantNr = customerIdByApplicantNr.Keys.ToDictionary(
                applicantNr => applicantNr,
                applicantNr => civicRegNumberParser.Value.Parse(customerProperties[customerIdByApplicantNr[applicantNr]]["civicRegNr"]));

            var ageInYearsByApplicantNr = customerIdByApplicantNr.Keys.ToDictionary(
                applicantNr => applicantNr,
                applicantNr => CivicRegNumbers.ComputeAgeFromCivicRegNumber(civicRegNrByApplicantNr[applicantNr], clock));

            var zipCodeByApplicantNr = customerIdByApplicantNr
                .Select(x => new { applicantNr = x.Key, customerId = x.Value, addressZipcode = customerProperties[x.Value].Opt("addressZipcode") })
                .Where(x => !string.IsNullOrWhiteSpace(x.addressZipcode))
                .ToDictionary(x => x.applicantNr, x => x.addressZipcode);

            return new CustomerData
            {
                CivicRegNrByApplicantNr = civicRegNrByApplicantNr,
                AgeInYearsByApplicantNr = ageInYearsByApplicantNr,
                ZipCodeByApplicantNr = zipCodeByApplicantNr
            };
        }

        protected class CustomerData
        {
            public Dictionary<int, ICivicRegNumber> CivicRegNrByApplicantNr { get; set; }
            public Dictionary<int, int?> AgeInYearsByApplicantNr { get; set; }
            public Dictionary<int, string> ZipCodeByApplicantNr { get; set; }
        }

        /// <summary>
        /// Calls credit-module to retrieve existing credits for the current customerids. Returns dictionary with applicantNr as key. 
        /// </summary>
        protected Dictionary<int, ApplicantCreditData> LoadCreditData(Dictionary<int, int> customerIdByApplicantNr)
        {
            var customerIds = customerIdByApplicantNr.Values.ToList();

            var loans = creditClient.GetCustomerCreditHistory(customerIds);

            int GetApplicantNr(int customerId) =>
                customerIdByApplicantNr.Single(x => x.Value == customerId).Key;

            return customerIds.ToDictionary(GetApplicantNr, id => new ApplicantCreditData
            {
                HasActiveLoanInSystem = loans.Any(loan => loan.Status == "Normal" && loan.CustomerIds.Any(cId => cId == id))
            });
        }


        protected class ApplicantCreditData
        {
            public bool HasActiveLoanInSystem { get; set; }
        }

        protected class VariableSetHelper
        {
            private readonly ISet<string> applicationVariableNames;
            private readonly ISet<string> applicantVariableNames;
            private readonly List<int> applicantNrs;
            private readonly bool forceLoadAllVariables;
            private readonly VariableSet result;

            public VariableSetHelper(
                ISet<string> applicationVariableNames,
                ISet<string> applicantVariableNames,
                List<int> applicantNrs,
                bool forceLoadAllVariables,
                VariableSet result)
            {
                this.applicationVariableNames = applicationVariableNames ?? new HashSet<string>();
                this.applicantVariableNames = applicantVariableNames ?? new HashSet<string>();
                this.applicantNrs = applicantNrs;
                this.forceLoadAllVariables = forceLoadAllVariables;
                this.result = result;
            }

            public void DoWhenApplicationVariableRequested(string name, Action handle)
            {
                if (forceLoadAllVariables || applicationVariableNames.Contains(name))
                {
                    handle();
                }
            }

            public void DoWhenApplicantVariableRequested(string name, Action<int> handleByApplicantNr)
            {
                if (forceLoadAllVariables || applicantVariableNames.Contains(name))
                {
                    applicantNrs.ForEach(applicantNr =>
                    {
                        handleByApplicantNr(applicantNr);
                    });
                }
            }

            public void HandleApplicationDecimalVariable(string name, Func<decimal?> getValue) =>
                DoWhenApplicationVariableRequested(name, () =>
                {
                    var value = getValue();
                    if (value.HasValue)
                        result.SetApplicationValue(name, value.Value);
                });

            public void HandleApplicationBoolVariable(string name, Func<bool?> getValue) =>
                DoWhenApplicationVariableRequested(name, () =>
                {
                    var value = getValue();
                    if (value.HasValue)
                        result.SetApplicationValue(name, value.Value);
                });

            public void HandleApplicationStringVariable(string name, Func<string> getValue) =>
                DoWhenApplicationVariableRequested(name, () =>
                {
                    var value = getValue();
                    if (value != null)
                        result.SetApplicationValue(name, value);
                });

            public void HandleApplicationIntVariable(string name, Func<int?> getValue) =>
                DoWhenApplicationVariableRequested(name, () =>
                {
                    var value = getValue();
                    if (value.HasValue)
                        result.SetApplicationValue(name, value.Value);
                });

            public void HandleApplicantIntVariable(string name, Func<int, int?> getValueByApplicantNr) =>
                DoWhenApplicantVariableRequested(name, applicantNr =>
                {
                    var value = getValueByApplicantNr(applicantNr);
                    if (value.HasValue)
                        result.SetApplicantValue(name, applicantNr, value.Value);
                });

            public void HandleApplicantStringVariable(string name, Func<int, string> getValueByApplicantNr) =>
                DoWhenApplicantVariableRequested(name, applicantNr =>
                {
                    var value = getValueByApplicantNr(applicantNr);
                    if (!string.IsNullOrWhiteSpace(value))
                        result.SetApplicantValue(name, applicantNr, value);
                });

            public void HandleApplicantBoolVariable(string name, Func<int, bool?> getValueByApplicantNr) =>
                DoWhenApplicantVariableRequested(name, applicantNr =>
                {
                    var value = getValueByApplicantNr(applicantNr);
                    if (value.HasValue)
                        result.SetApplicantValue(name, applicantNr, value.Value);
                });

            public void HandleCoApplicantOnly(string name, Action<string> handleWhenHasCoApplicant)
            {
                if (applicantNrs.Count > 1)
                    handleWhenHasCoApplicant(name);
                else if (applicantVariableNames.Contains(name))
                    result.SetApplicationValue(name, "noCoApp");
            }
        }
    }
}