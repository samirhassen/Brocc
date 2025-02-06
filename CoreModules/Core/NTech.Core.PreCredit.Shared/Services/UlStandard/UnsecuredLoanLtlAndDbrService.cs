using nPreCredit.Code.Services.SharedStandard;
using NTech.Banking.CivicRegNumbers;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services.UnsecuredLoans
{
    public class UnsecuredLoanLtlAndDbrService
    {
        private readonly IPreCreditContextFactoryService contextFactoryService;
        private readonly ICustomerClient customerClient;
        private readonly ILtlDataTables ltlDataTables;
        private readonly Lazy<CivicRegNumberParser> civicRegNumberParser;

        public UnsecuredLoanLtlAndDbrService(IPreCreditContextFactoryService contextFactoryService, IClientConfigurationCore clientConfiguration, ICustomerClient customerClient,
            ILtlDataTables ltlDataTables)
        {
            this.contextFactoryService = contextFactoryService;
            this.customerClient = customerClient;
            this.ltlDataTables = ltlDataTables;
            civicRegNumberParser = new Lazy<CivicRegNumberParser>(() => new CivicRegNumberParser(clientConfiguration.Country.BaseCountry));
        }

        public (LoanStandardLtlResult LeftToLiveOn, decimal? Dbr, string DbrMissingReason) CalculateLeftToLiveOnAndDbr(string applicationNr)
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
                var customerProperties = customerClient.BulkFetchPropertiesByCustomerIdsD(customerIdByApplicantNr.Values.ToHashSetShared(), "civicRegNr");
                var ageInYearsByApplicantNr = customerIdByApplicantNr.Keys.ToDictionary(
                    applicantNr => applicantNr,
                    applicantNr => CivicRegNumbers.ComputeAgeFromCivicRegNumber(customerProperties[customerIdByApplicantNr[applicantNr]]["civicRegNr"], context.CoreClock, civicRegNumberParser.Value));

                var ltlService = new LoanStandardLtlService(ltlDataTables);
                var ltl = ltlService.CalculateLeftToLiveOnForUnsecuredLoan(applicationList, applicantList, ageInYearsByApplicantNr, householdChildrenList, loansToSettleList);

                string dbrMissingReason = null;
                var dbrAmount = CalculateDbr(applicationList, applicantList, loansToSettleList, x => dbrMissingReason = x);

                return (LeftToLiveOn: ltl, Dbr: dbrAmount, DbrMissingReason: dbrMissingReason);
            }
        }

        public decimal? CalculateDbr(ComplexApplicationList applicationList, ComplexApplicationList applicantList, ComplexApplicationList loansToSettleList, Action<string> observeUndefinedReason)
        {
            var yearlyIncomeAmount = 12m * applicantList.GetRows().Select(x => x.GetUniqueItemDecimal("incomePerMonthAmount") ?? 0m).Sum();
            if (yearlyIncomeAmount <= 0m)
            {
                observeUndefinedReason?.Invoke("No income");
                return null;
            }
            //We dont count loans to be settled since requestedLoanAmount is assumed to be settled loans + paidToCustomerAmount
            var currentLoansNotToBeSettledAmount = loansToSettleList.GetRows().Select(x => x.GetUniqueItem("shouldBeSettled") == "true"
                ? 0m
                : x.GetUniqueItemDecimal("currentDebtAmount") ?? 0m).Sum();
            var settledLoansAndPaidToCustomerAmount = applicationList.GetRow(1, true).GetUniqueItemDecimal("requestedLoanAmount") ?? 0m;

            return Math.Round((currentLoansNotToBeSettledAmount + settledLoansAndPaidToCustomerAmount) / yearlyIncomeAmount, 4);
        }
    }
}