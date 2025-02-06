using nCredit;
using nCredit.DbModel.DomainModel;
using nCredit.DbModel.Repository;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Core.Credit.Shared.Repository
{
    public class CustomerCreditHistoryCoreRepository
    {
        private readonly CreditContextFactory creditContextFactory;
        private readonly INotificationProcessSettingsFactory notificationSettingsFactory;
        private readonly ICreditEnvSettings envSettings;

        public CustomerCreditHistoryCoreRepository(CreditContextFactory creditContextFactory, INotificationProcessSettingsFactory notificationSettingsFactory,
            ICreditEnvSettings envSettings)
        {
            this.creditContextFactory = creditContextFactory;
            this.notificationSettingsFactory = notificationSettingsFactory;
            this.envSettings = envSettings;
        }

        public List<Credit> GetCustomerCreditHistory(List<int> customerIds, List<string> creditNrs, Action<string> observeSqlQuery = null)
        {
            var repo = new PartialCreditModelRepository();

            using (var context = creditContextFactory.CreateContext())
            {
                var sixMonthsBackDate = context.CoreClock.Today.AddMonths(-6);
                var selectItems = new Dictionary<string, string>
                {
                    [nameof(Credit.CreditNr)] = "c.CreditNr",
                    [nameof(Credit.NrOfApplicants)] = "c.NrOfApplicants",
                    [nameof(Credit.ProviderName)] = "c.ProviderName",
                    [nameof(Credit.CapitalBalance)] = CommonReportingTableExpressions.GetDataPointExpression(CommonReportingDataPointCode.CurrentCapitalBalance),
                    [nameof(Credit.StartDate)] = "c.StartDate",
                    [nameof(Credit.MarginInterestRatePercent)] = CommonReportingTableExpressions.GetDataPointExpression(CommonReportingDataPointCode.CurrentMarginInterestRate),
                    [nameof(Credit.ReferenceInterestRatePercent)] = CommonReportingTableExpressions.GetDataPointExpression(CommonReportingDataPointCode.CurrentReferenceInterestRate),
                    [nameof(Credit.AnnuityAmount)] = CommonReportingTableExpressions.GetDataPointExpression(CommonReportingDataPointCode.CurrentAnnuityAmount),
                    [nameof(Credit.NotificationFeeAmount)] = CommonReportingTableExpressions.GetDataPointExpression(CommonReportingDataPointCode.CurrentNotificationFee),
                    [nameof(Credit.CurrentlyOverdueSinceDate)] = CommonReportingTableExpressions.GetDataPointExpression(CommonReportingDataPointCode.CurrentOverdueSinceDate),
                    [nameof(Credit.IsOrHasBeenOnDebtCollection)] = @"(case when exists
                        (select 1 from DatedCreditString dc where dc.CreditNr = c.CreditNr and dc.[Name] = 'CreditStatus' and dc.[Value] = 'SentToDebtCollection' and dc.TransactionDate <= @toDate)
                        then 1 else 0 end)",
                    [nameof(Credit.IsMortgageLoan)] = "case when c.CreditType = 'MortgageLoan' then 1 else 0 end",
                    [nameof(Credit.Status)] = CommonReportingTableExpressions.GetDataPointExpression(CommonReportingDataPointCode.CurrentCreditStatus),
                    [nameof(Credit.ApplicationNr)] = CommonReportingTableExpressions.GetDataPointExpression(CommonReportingDataPointCode.CurrentApplicationNr),
                    [nameof(Credit.MaxNrOfDaysBetweenDueDateAndPaymentEver)] = "select isnull(max(cn.NrOfDaysBetweenDueDateAndPayment), 0) from CreditNotificationPeriodEnd cn where cn.CreditNr = c.CreditNr",
                    [nameof(Credit.MaxNrOfDaysBetweenDueDateAndPaymentLastSixMonths)] = "select isnull(max(cn.NrOfDaysBetweenDueDateAndPayment), 0) from CreditNotificationPeriodEnd cn where cn.CreditNr = c.CreditNr and cn.TransactionDate >= @sixMonthsBackDate",
                    [nameof(Credit.NrOfClosedNotifications)] = "select count(*) from CreditNotificationPeriodEnd cn where cn.CreditNr = c.CreditNr and cn.ClosedTransactionDate <= @toDate",
                    [nameof(Credit.InitialCapitalBalance)] = CommonReportingTableExpressions.GetDataPointExpression(CommonReportingDataPointCode.InitialCapitalBalance),
                    [nameof(Credit.InitialAnnuityAmount)] = CommonReportingTableExpressions.GetDataPointExpression(CommonReportingDataPointCode.InitialAnnuityAmount),
                    [nameof(Credit.InitialMarginInterestRatePercent)] = CommonReportingTableExpressions.GetDataPointExpression(CommonReportingDataPointCode.InitialMarginInterestRate),
                    [nameof(Credit.InitialReferenceInterestRatePercent)] = CommonReportingTableExpressions.GetDataPointExpression(CommonReportingDataPointCode.InitialReferenceInterestRate),
                    [nameof(Credit.InitialCapitalizedInitialFeeAmount)] = CommonReportingTableExpressions.GetDataPointExpression(CommonReportingDataPointCode.InitialCapitalizedInitialFee),
                    [nameof(Credit.InitialNotificationFeeAmount)] = CommonReportingTableExpressions.GetDataPointExpression(CommonReportingDataPointCode.InitialNotificationFee),
                }.Select(x => $"({x.Value}) as {x.Key}");

                var query = $"select {string.Join($", {Environment.NewLine}", selectItems)} from DimensionCredit c where 1=1";
                
                if (customerIds != null)
                    query += " and exists (select 1 from CreditCustomer u where u.CreditNr = c.CreditNr and u.CustomerId in @customerIds)";

                if (creditNrs != null)
                    query += " and c.CreditNr in @creditNrs";

                var graceDays = notificationSettingsFactory.GetByCreditType(envSettings.ClientCreditType).NotificationOverDueGraceDays;
                var credits = CommonReportingTableExpressions.Query<Credit>(context.GetConnection(), query, context.CoreClock.Today, graceDays,
                    addExtraParameters: x => 
                    {
                        x["sixMonthsBackDate"] = sixMonthsBackDate;
                        if (creditNrs != null)
                            x["creditNrs"] = creditNrs;
                        if (customerIds != null)
                            x["customerIds"] = customerIds;
                    }, observeSqlQuery: observeSqlQuery);

                var allCreditNrs = credits.Select(x => x.CreditNr).Distinct().ToArray();
                Dictionary<string, List<int>> customerIdsByCreditNr = new Dictionary<string, List<int>>(allCreditNrs.Length);
                foreach(var creditNrGroup in allCreditNrs.SplitIntoGroupsOfN(250))
                {
                    var creditsInGroup = context
                        .CreditHeadersQueryable
                        .Where(x => creditNrGroup.Contains(x.CreditNr))
                        .Select(x => new
                        {
                            x.CreditNr,
                            CustomerIds = x.CreditCustomers.Select(y => y.CustomerId)
                        })
                        .ToList();
                    foreach(var credit in creditsInGroup)
                    {
                        customerIdsByCreditNr[credit.CreditNr] = credit.CustomerIds.ToList();
                    }
                }
                foreach(var credit in credits)
                {
                    credit.CustomerIds = customerIdsByCreditNr.Opt(credit.CreditNr);
                }

                return credits;
            }
        }


        public class Credit
        {
            public string CreditNr { get; set; }
            public int NrOfApplicants { get; set; }
            public string ProviderName { get; set; }
            public DateTimeOffset StartDate { get; set; }
            public List<int> CustomerIds { get; set; }
            public decimal CapitalBalance { get; set; }
            public decimal? MarginInterestRatePercent { get; set; }
            public decimal ReferenceInterestRatePercent { get; set; }
            public decimal? AnnuityAmount { get; set; }
            public decimal? NotificationFeeAmount { get; set; }
            public DateTime? CurrentlyOverdueSinceDate { get; set; }
            public int? MaxNrOfDaysBetweenDueDateAndPaymentEver { get; set; }
            public int? MaxNrOfDaysBetweenDueDateAndPaymentLastSixMonths { get; set; }
            public bool IsOrHasBeenOnDebtCollection { get; set; }
            public int NrOfClosedNotifications { get; set; }
            public string ApplicationNr { get; set; }
            public string Status { get; set; }
            public bool IsMortgageLoan { get; set; }
            public decimal? InitialCapitalBalance { get; set; }
            public decimal? InitialAnnuityAmount { get; set; }
            public decimal? InitialMarginInterestRatePercent { get; set; }
            public decimal? InitialReferenceInterestRatePercent { get; set; }
            public decimal? InitialCapitalizedInitialFeeAmount { get; set; }
            public decimal? InitialNotificationFeeAmount { get; set; }
        }
    }
}