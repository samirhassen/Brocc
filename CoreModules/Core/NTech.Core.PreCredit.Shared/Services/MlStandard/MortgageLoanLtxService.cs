using nPreCredit.Code.Services.SharedStandard;
using NTech.Banking.CivicRegNumbers;
using NTech.Core;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure.CreditStandard;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    /// <summary>
    /// Calculate LTI (loan to income), LTV (loan to value)  and LTL (left to live on).
    /// </summary>
    public class MortgageLoanLtxService
    {
        private readonly Lazy<CivicRegNumberParser> civicRegNumberParser;

        public MortgageLoanLtxService(ICoreClock clock, IPreCreditContextFactoryService preCreditContextFactoryService, ICustomerClient customerClient,
            IClientConfigurationCore clientConfiguration, ILtlDataTables ltlDataTables)
        {
            this.clock = clock;
            this.preCreditContextFactoryService = preCreditContextFactoryService;
            this.customerClient = customerClient;
            this.ltlDataTables = ltlDataTables;
            civicRegNumberParser = new Lazy<CivicRegNumberParser>(() => new CivicRegNumberParser(clientConfiguration.Country.BaseCountry));
        }

        private readonly ICoreClock clock;
        private readonly IPreCreditContextFactoryService preCreditContextFactoryService;
        private readonly ICustomerClient customerClient;
        private readonly ILtlDataTables ltlDataTables;

        public (LoanStandardLtlResult LeftToLiveOn, decimal? Lti, string LtiMissingReason, decimal? Ltv, string LtvMissingReason) CalculateAll(string applicationNr)
        {
            using (var context = preCreditContextFactoryService.CreateExtended())
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
                var customerProperties = customerClient.BulkFetchPropertiesByCustomerIdsD(customerIdByApplicantNr.Values.ToHashSetShared(), "civicRegNr");
                var ageInYearsByApplicantNr = customerIdByApplicantNr.Keys.ToDictionary(
                    applicantNr => applicantNr,
                    applicantNr => CivicRegNumbers.ComputeAgeFromCivicRegNumber(customerProperties[customerIdByApplicantNr[applicantNr]]["civicRegNr"], clock, civicRegNumberParser.Value));

                string ltiMissingReason = null;
                var lti = CalculateLoanToIncome(applicationList, applicantList, mortgageLoansToSettleList, loansToSettleList, x => ltiMissingReason = x);

                string ltvMissingReason = null;
                var ltv = CalculateLoanToValue(applicationList, mortgageLoansToSettleList, x => ltvMissingReason = x);

                var ltlResult = new LoanStandardLtlService(ltlDataTables).CalculateLeftToLiveOnForMortgageLoan(
                    applicationList, applicantList, ageInYearsByApplicantNr, householdChildrenList,
                    loansToSettleList, mortgageLoansToSettleList);

                return (LeftToLiveOn: ltlResult, Lti: lti, LtiMissingReason: ltiMissingReason, Ltv: ltv, LtvMissingReason: ltvMissingReason);
            }
        }

        public static decimal? CalculateLoanToIncome(
            ComplexApplicationList applicationList,
            ComplexApplicationList applicantList,
            ComplexApplicationList mortgageLoansToSettleList,
            ComplexApplicationList loansToSettleList,
            Action<string> observeUndefinedReason)
        {
            var houseHoldYearlyIncome = 12m * applicantList
               .GetRows()
               .Sum(x => x.GetUniqueItemDecimal("incomePerMonthAmount") ?? 0m);
            if (houseHoldYearlyIncome <= 0m)
            {
                observeUndefinedReason?.Invoke("No income");
                return null;
            }

            var propertyMortgageLoansAmount = mortgageLoansToSettleList
                .GetRows()
                .Where(x => x.GetUniqueItemBoolean("shouldBeSettled") == false)
                .Sum(x => x.GetUniqueItemDecimal("currentDebtAmount") ?? 0m);

            //NOTE: shouldBeSettled is currently not used at all for ML so the != true check is just to ensure this doesnt break if that gets added
            var otherMortgageLoansAmount = loansToSettleList
                .GetRows()
                .Where(x => x.GetUniqueItem("loanType") == CreditStandardOtherLoanType.Code.mortgage.ToString() && x.GetUniqueItemBoolean("shouldBeSettled") != true)
                .Sum(x => x.GetUniqueItemDecimal("currentDebtAmount") ?? 0m);

            var requestedLoanAmount = CalculateRequestedLoanAmount(applicationList, mortgageLoansToSettleList);

            var totalLoansAmount = propertyMortgageLoansAmount + otherMortgageLoansAmount + requestedLoanAmount;

            return Math.Round(totalLoansAmount / houseHoldYearlyIncome, 3);
        }

        public static decimal? CalculateLoanToValue(
            ComplexApplicationList applicationList,
            ComplexApplicationList mortgageLoansToSettleList,
            Action<string> observeUndefinedReason)
        {
            var objectValueAmount = applicationList.GetRow(1, true).GetUniqueItemDecimal("objectValueAmount");
            if ((objectValueAmount ?? 0m) <= 0m)
            {
                observeUndefinedReason?.Invoke("Missing object value");
                return null;
            }
            var requestedLoanAmount = CalculateRequestedLoanAmount(applicationList, mortgageLoansToSettleList);
            var nonSettledLoanAmount = mortgageLoansToSettleList
                    .GetRows()
                    .Where(x => x.GetUniqueItemBoolean("shouldBeSettled") == false)
                    .Sum(x => x.GetUniqueItemDecimal("currentDebtAmount") ?? 0m);
            return Math.Round((requestedLoanAmount + nonSettledLoanAmount) / objectValueAmount.Value, 2);
        }

        public static decimal CalculateRequestedLoanAmount(ComplexApplicationList applicationList, ComplexApplicationList mortgageLoansToSettleList)
        {
            var applicationRow = applicationList.GetRow(1, true);
            return
                (applicationRow.GetUniqueItemDecimal("objectPriceAmount") ?? 0m)
                + mortgageLoansToSettleList
                    .GetRows()
                    .Where(x => x.GetUniqueItemBoolean("shouldBeSettled") == true)
                    .Sum(x => x.GetUniqueItemDecimal("currentDebtAmount") ?? 0m)
                + (applicationRow.GetUniqueItemDecimal("paidToCustomerAmount") ?? 0m)
                - (applicationRow.GetUniqueItemDecimal("ownSavingsAmount") ?? 0m);
        }
    }
}