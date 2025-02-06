using Dapper;
using nCredit;
using nCredit.Code.Services;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Credit.Shared.Models;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services.SwedishMortgageLoans
{
    public class AnnexTwoEsmaReportService
    {
        private readonly CreditContextFactory creditContextFactory;
        private readonly INotificationProcessSettingsFactory notificationProcessSettingsFactory;
        private readonly IClientConfigurationCore clientConfiguration;

        public AnnexTwoEsmaReportService(CreditContextFactory creditContextFactory, INotificationProcessSettingsFactory notificationProcessSettingsFactory,
            IClientConfigurationCore clientConfiguration)
        {
            this.creditContextFactory = creditContextFactory;
            this.notificationProcessSettingsFactory = notificationProcessSettingsFactory;
            this.clientConfiguration = clientConfiguration;
        }

        /// <summary>
        /// This could just be a big hardcoded string but we add some extra things here
        /// to connect it back to the typesystem so addign and removing properties gets less
        /// error prone.
        /// 
        /// Also kind of a proof of concept of letting the user pick and choose which fields to fetch.
        /// </summary>
        private static Lazy<string> Query = new Lazy<string>(() =>
        {
            var t = new EsmaAnnexTwoLoan { };
            var d = new Dictionary<string, string>
            {
                [nameof(t.CreditNr)] = "c.CreditNr",
                [nameof(t.CreatedDate)] = "c.CreatedTransactionDate",
                [nameof(t.MainCustomerId)] = "c.Applicant1CustomerId",
                [nameof(t.LoanOwnerName)] = "(select d.[Value] from DatedCreditStringPeriodEnd d where d.CreditNr = c.CreditNr and d.[Name] = 'LoanOwner')",
                [nameof(t.LoanOwnerDate)] = "(select d.TransactionDate from DatedCreditStringPeriodEnd d where d.CreditNr = c.CreditNr and d.[Name] = 'LoanOwner')",
                [nameof(t.ClosedDate)] = "(select d.TransactionDate from DatedCreditStringPeriodEnd d where d.CreditNr = c.CreditNr and d.[Name] = 'CreditStatus' and d.[Value] <> 'Normal')",
                [nameof(t.ClosedStatus)] = "(select d.[Value] from DatedCreditStringPeriodEnd d where d.CreditNr = c.CreditNr and d.[Name] = 'CreditStatus' and d.[Value] <> 'Normal')",
                [nameof(t.InitialRepaymentTimeInMonths)] = "(select d.[Value] from CreditCreatedDatedCreditValue d where d.CreditNr = c.CreditNr and d.[Name] = 'InitialRepaymentTimeInMonths')",
                [nameof(t.InitialCapitalDebt)] = "(select isnull(sum(t.Amount), 0) from CreditCreatedAccountTransaction t where t.AccountCode = 'CapitalDebt' and t.CreditNr = c.CreditNr)",
                [nameof(t.CurrentCapitalDebt)] = CommonReportingTableExpressions.GetDataPointExpression(CommonReportingDataPointCode.CurrentCapitalBalance),
                [nameof(t.CurrentNotNotifiedCapitalDebt)] = "(select isnull(sum(t.Amount), 0) from AccountTransactionPeriodEnd t where t.AccountCode = 'NotNotifiedCapital' and t.CreditNr = c.CreditNr)",
                [nameof(t.CurrentAmortizationExceptionUntilDate)] = "(select d.[Value] from DatedCreditDatePeriodEnd d where d.CreditNr = c.CreditNr and d.[Name] = 'AmortizationExceptionUntilDate')",
                [nameof(t.CurrentExceptionAmortizationAmount)] = "(select d.[Value] from DatedCreditValuePeriodEnd d where d.CreditNr = c.CreditNr and d.[Name] = 'ExceptionAmortizationAmount')",
                [nameof(t.CurrentMonthlyAmortizationAmount)] = CommonReportingTableExpressions.GetDataPointExpression(CommonReportingDataPointCode.CurrentMonthlyAmortizationAmount),
                [nameof(t.LatestNotificationDueDate)] = "(select max(h.DueDate) from CreditNotificationHeader h where h.CreditNr = c.CreditNr and h.TransactionDate <= @toDate)",
                [nameof(t.CurrentMarginInterestRate)] = CommonReportingTableExpressions.GetDataPointExpression(CommonReportingDataPointCode.CurrentMarginInterestRate),
                [nameof(t.CurrentReferenceInterestRate)] = CommonReportingTableExpressions.GetDataPointExpression(CommonReportingDataPointCode.CurrentReferenceInterestRate),
                [nameof(t.CurrentInterestRebindMonthCount)] = "(select d.[Value] from DatedCreditValuePeriodEnd d where d.CreditNr = c.CreditNr and d.[Name] = 'MortgageLoanInterestRebindMonthCount')",
                [nameof(t.NextInterestRebindDate)] = "(select d.[Value] from DatedCreditDatePeriodEnd d where d.CreditNr = c.CreditNr and d.[Name] = 'MortgageLoanNextInterestRebindDate')",
                [nameof(t.CurrentLoanAgreementEndDate)] = "(select d.[Value] from DatedCreditDatePeriodEnd d where d.CreditNr = c.CreditNr and d.[Name] = 'MortgageLoanEndDate')",
                [nameof(t.LatestExtraAmortizationDate)] = "(select top 1 t.TransactionDate from AccountTransactionPeriodEnd t where t.AccountCode = 'CapitalDebt' and t.CreditNr = c.CreditNr and t.IncomingPaymentId is not null and t.WriteoffId is null and t.CreditNotificationId is null order by t.TransactionDate desc)",
                [nameof(t.LatestTermChangeDate)] = "(select top 1 t.TransactionDate from CommitedByEventIdCreditTermChange t where t.CreditNr = c.CreditNr order by t.TransactionDate desc)",
                [nameof(t.LatestMissedDueDate)] = "(select top 1 n.DueDate from CreditNotificationPeriodEnd n where n.CreditNr = c.CreditNr and n.IsLateOrWasPaidLate = 1 order by n.TransactionDate desc)",
                [nameof(t.CurrentNrOfOverdueDays)] = CommonReportingTableExpressions.GetDataPointExpression(CommonReportingDataPointCode.CurrentNrOfOverdueDays),
                [nameof(t.DebtCollectionExportCapitalAmount)] = "(select top 1 isnull(e.WrittenOffCapitalAmount, 0) from CreditDebtCollectionExport e where e.CreditNr = c.CreditNr)",
                [nameof(t.TotalWrittenOffCapitalAmount)] = "(select -isnull(sum(t.Amount), 0) from AccountTransactionPeriodEnd t where t.AccountCode = 'CapitalDebt' and t.CreditNr = c.CreditNr and t.WriteoffId is not null)",
                [nameof(t.TotalWrittenOffInterestAmount)] = "(select -isnull(sum(t.Amount), 0) from AccountTransactionPeriodEnd t where t.AccountCode = 'InterestDebt' and t.CreditNr = c.CreditNr and t.WriteoffId is not null)",
                [nameof(t.TotalWrittenOffFeesAmount)] = "(select -isnull(sum(t.Amount), 0) from AccountTransactionPeriodEnd t where t.IsFee = 1 and t.CreditNr = c.CreditNr and t.WriteoffId is not null)",
                [nameof(t.CollateralId)] = "c.CollateralHeaderId",
                [nameof(t.CollateralTypeCode)] = "(select top 1 i.StringValue from CollateralItemPeriodEnd i where i.CollateralHeaderId = c.CollateralHeaderId and i.ItemName = 'objectTypeCode')"
            };
            var nl = Environment.NewLine;
            return "select " +  string.Join($", {nl}", d.Select(x => $"{x.Value} as [{x.Key}]")) + $"{nl}from DimensionCredit c";
        });
      
        public EsmaAnnexTwoReportResponse GetAnnexTwoReportData(FromDateToDateReportRequest request, Action<string> observeSqlQuery = null)
        {
            using(var context = creditContextFactory.CreateContext())
            {
                DateTime fromDate;
                DateTime toDate;
                if(request.FromDate.HasValue && request.ToDate.HasValue)
                {
                    fromDate = request.FromDate.Value;
                    toDate = request.ToDate.Value;
                }
                else if(request.FromDate.HasValue != request.ToDate.HasValue)
                {
                    throw new NTechCoreWebserviceException("Either both or none of FromDate and ToDate must be given") { IsUserFacing = true, ErrorHttpStatusCode = 400 };
                }
                else
                {
                    //Default to all of last month
                    var thisMonth = Month.ContainingDate(context.CoreClock.Today);
                    fromDate = thisMonth.FirstDate;
                    toDate = thisMonth.LastDate;
                }

                var graceDays = SwedishMortgageLoanReportData.GetGraceDays(clientConfiguration);
                var loans = CommonReportingTableExpressions.Query<EsmaAnnexTwoLoan>(context.GetConnection(), Query.Value, toDate, graceDays,
                    addExtraParameters: x => x["fromDate"] = fromDate,
                    observeSqlQuery: observeSqlQuery);

                var notificationSettings = notificationProcessSettingsFactory.GetByCreditType(CreditType.MortgageLoan);
                foreach (var loan in loans)
                {
                    if (!loan.ClosedDate.HasValue)
                    {
                        DateTime? nextDueDate = null;
                        loan.CurrentLastAmortizationPlanDueDate = FixedDueDayAmortizationPlanCalculator.CalculateEndDateForFixedPaymentMortgageLoan(
                            lastDueDate: loan.LatestNotificationDueDate,
                            monthlyFixedCapitalAmount: loan.CurrentMonthlyAmortizationAmount,
                            endDate: loan.CurrentLoanAgreementEndDate,
                            notNotifiedCapitalAmountAfterLastNotification: loan.CurrentNotNotifiedCapitalDebt,
                            amortizationExceptionUntilDate: loan.CurrentAmortizationExceptionUntilDate,
                            amortizationExceptionAmount: loan.CurrentExceptionAmortizationAmount,
                            today: toDate,
                            notificationProcessSettings: notificationSettings,
                            closedDate: loan.ClosedDate,
                            observeNextDueDate: x => nextDueDate = x);

                        loan.NextFutureNotificationDueDate = loan.LatestNotificationDueDate.HasValue && loan.LatestNotificationDueDate.Value >= toDate
                            ? loan.LatestNotificationDueDate
                            : nextDueDate;
                    }

                    if (loan.CurrentAmortizationExceptionUntilDate.HasValue && loan.CurrentAmortizationExceptionUntilDate.Value < toDate)
                    {
                        //The system has logic to handle the other case internally but we remove these passed dates here to not force our users
                        //to implement this logic also.
                        loan.CurrentAmortizationExceptionUntilDate = null;
                        loan.CurrentExceptionAmortizationAmount = null;
                    }
                }
                foreach(var loanGroup in loans.ToArray().SplitIntoGroupsOfN(200))
                {
                    var loanGroupCollateralIds = loanGroup.Select(x => x.CollateralId).ToHashSetShared();

                    var creditCreatedBasisByCollateralId = GetAmortizationBasis(loanGroupCollateralIds, context, toDate, graceDays, true);
                    var amortizationBasisPeriodEndByCollateralId = GetAmortizationBasis(loanGroupCollateralIds, context, toDate, graceDays, false);

                    foreach(var loan in loanGroup)
                    {
                        var amortizationBasisPeriodEnd = amortizationBasisPeriodEndByCollateralId.Opt(loan.CollateralId);
                        if (amortizationBasisPeriodEnd != null)
                        {
                            loan.CurrentAmortizationBasisCollateralValue = amortizationBasisPeriodEnd.ObjectValue;
                            loan.CurrentAmortizationBasisCollateralValueDate = amortizationBasisPeriodEnd.ObjectValueDate;
                        }

                        var amortizationBasisCreditCreated = creditCreatedBasisByCollateralId.Opt(loan.CollateralId);
                        if(amortizationBasisCreditCreated != null)
                        {
                            loan.InitialAmortizationBasiLtvFraction = amortizationBasisCreditCreated.LtvFraction;
                            loan.InitialAmortizationBasisCollateralValue = amortizationBasisCreditCreated.ObjectValue;
                            loan.InitialAmortizationBasisCollateralValueDate = amortizationBasisCreditCreated.ObjectValueDate;
                        }
                    }
                }

                return new EsmaAnnexTwoReportResponse
                {
                    Loans = loans
                };                
            }            
        }
        
        private Dictionary<int, SwedishMortgageLoanAmortizationBasisModel> GetAmortizationBasis(HashSet<int> collateralIds, ICreditContextExtended context, DateTime toDate, 
            int graceDays, bool isCreditCreated)
        {
            var sqlQuery =
@"select	c.CollateralHeaderId, c.StringValue as SeMlAmortBasisKeyValueItemKey
from	[[[SOURCE_NAME]] c
where	c.CollateralHeaderId in @collateralIds
and		c.ItemName = 'seMlAmortBasisKeyValueItemKey'".Replace("[[[SOURCE_NAME]]", isCreditCreated ? "CreditCreatedCollateralItem" : "CollateralItemPeriodEnd");

            var collateralIdByBasisKey = CommonReportingTableExpressions.Query<SeMlAmortBasisKeyValueItemKeyTemp>(context.GetConnection(), sqlQuery, toDate, graceDays,
                addExtraParameters: x =>
                {
                    x["collateralIds"] = collateralIds;
                })
                .ToDictionary(x => x.SeMlAmortBasisKeyValueItemKey, x => x.CollateralHeaderId);
            var result = new Dictionary<int, SwedishMortgageLoanAmortizationBasisModel>(collateralIds.Count);

            var basisKeyValueItems = context
                .KeyValueItemsQueryable
                .Where(x => x.KeySpace == KeyValueStoreKeySpaceCode.SeMortgageLoanAmortzationBasisV1.ToString() && collateralIdByBasisKey.Keys.Contains(x.Key))
                .Select(x => new { x.Key, x.Value })
                .ToList();

            foreach(var basisKeyValueItem in basisKeyValueItems)
            {
                result[collateralIdByBasisKey[basisKeyValueItem.Key]] = SwedishMortgageLoanAmortizationBasisStorageModel.Parse(basisKeyValueItem.Value).Model;
            }
            
            return result;
        }

        private class SeMlAmortBasisKeyValueItemKeyTemp
        {
            public int CollateralHeaderId { get; set; }
            public string SeMlAmortBasisKeyValueItemKey { get; set; }
        }
    }

    public class EsmaAnnexTwoReportResponse
    {
        public List<EsmaAnnexTwoLoan> Loans { get; set; }
    }
}
