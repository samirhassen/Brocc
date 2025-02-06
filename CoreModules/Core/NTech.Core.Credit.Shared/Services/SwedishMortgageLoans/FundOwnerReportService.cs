using Dapper;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Credit.Shared.Models;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services.SwedishMortgageLoans
{
    public class FundOwnerReportService
    {
        private readonly CreditContextFactory creditContextFactory;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly ICustomerClient customerClient;

        public FundOwnerReportService(CreditContextFactory creditContextFactory, IClientConfigurationCore clientConfiguration, ICustomerClient customerClient)
        {
            this.creditContextFactory = creditContextFactory;
            this.clientConfiguration = clientConfiguration;
            this.customerClient = customerClient;
        }

        private static Lazy<string> Query = new Lazy<string>(() =>
        {
            string GetInPeriodPaidNotificationFeeAmount() =>
@"(select -isnull(sum(p.Amount), 0)
 from    AccountTransactionPeriodEnd p 
 where   p.CreditNr = c.CreditNr 
 and     p.IncomingPaymentId is not null 
 and     p.AccountCode = 'NotificationFeeDebt'
 and     p.TransactionDate >= @fromDate)"
    .Replace(Environment.NewLine, " ");

            string GetInPeriodExtraAmortizationAmount() =>
@"(select -isnull(sum(p.Amount), 0)
 from    AccountTransactionPeriodEnd p 
 where   p.CreditNr = c.CreditNr 
 and     p.IncomingPaymentId is not null 
 and     p.AccountCode = 'CapitalDebt'
 and     p.CreditNotificationId is null
 and     p.TransactionDate >= @fromDate)"
    .Replace(Environment.NewLine, " ");

            var t = new FundOwnerReportLoan { };
            var d = new Dictionary<string, string>
            {
                [nameof(t.CreditNr)] = "c.CreditNr",
                [nameof(t.MainCustomerId)] = "c.Applicant1CustomerId",
                [nameof(t.InPeriodPaidNotificationFeeAmount)] = GetInPeriodPaidNotificationFeeAmount(),
                [nameof(t.InPeriodExtraAmortizationAmount)] = GetInPeriodExtraAmortizationAmount(),
                [nameof(t.LoanOwnerName)] = "(select d.[Value] from DatedCreditStringPeriodEnd d where d.CreditNr = c.CreditNr and d.[Name] = 'LoanOwner')",
                [nameof(t.CollateralZipcode)] = "(select top 1 i.StringValue from CollateralItemPeriodEnd i where i.CollateralHeaderId = c.CollateralHeaderId and i.ItemName = 'objectAddressZipcode')"
            };
            var nl = Environment.NewLine;
            return "select " + string.Join($", {nl}", d.Select(x => $"{x.Value} as [{x.Key}]")) + $"{nl}from DimensionCredit c";
        });

        public FundOwnerReportResponse GetFundOwnerReportData(FromDateToDateReportRequest request, Action<string> observeSqlQuery = null)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                DateTime fromDate;
                DateTime toDate;
                if (request.FromDate.HasValue && request.ToDate.HasValue)
                {
                    fromDate = request.FromDate.Value;
                    toDate = request.ToDate.Value;
                }
                else if (request.FromDate.HasValue != request.ToDate.HasValue)
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

                var loans = CommonReportingTableExpressions.Query<FundOwnerReportLoan>(context.GetConnection(), Query.Value, toDate, graceDays,
                    addExtraParameters: x => x["fromDate"] = fromDate,
                    observeSqlQuery: observeSqlQuery);

                foreach (var loanGroup in loans.ToArray().SplitIntoGroupsOfN(200))
                {
                    var zipCodeByCustomerId = customerClient.BulkFetchPropertiesByCustomerIdsD(loanGroup.Select(x => x.MainCustomerId).ToHashSetShared(), "addressZipcode");
                    foreach(var loan in loanGroup)
                    {
                        loan.MainCustomerAddressZipcode = zipCodeByCustomerId?.Opt(loan.MainCustomerId)?.Opt("addressZipcode");
                    }
                }

                return new FundOwnerReportResponse
                {
                    Loans = loans
                };
            }
        }
    }

    public class FundOwnerReportResponse
    {
        public List<FundOwnerReportLoan> Loans { get; set; }
    }

    public class FundOwnerReportLoan
    {
        /// <summary>
        /// Credit nr/loan nr
        /// </summary>
        public string CreditNr { get; set; }

        /// <summary>
        /// Paid notification fee during the period.
        /// </summary>
        public decimal InPeriodPaidNotificationFeeAmount { get; set; }

        /// <summary>
        /// Paid capital/principal during the period beyond that was not placed against a notification.
        /// </summary>
        public decimal InPeriodExtraAmortizationAmount { get; set; }

        /// <summary>
        /// Loan owner name at the end of the period
        /// </summary>
        public string LoanOwnerName { get; set; }

        /// <summary>
        /// Original/New Obligor Identifier.
        /// </summary>
        public int MainCustomerId { get; set; }
        /// <summary>
        /// The first customers zip code at the end of the period
        /// </summary>
        public string MainCustomerAddressZipcode { get; set; }

        /// <summary>
        /// The collaterals zipcode
        /// </summary>
        public string CollateralZipcode { get; set; }
    }
}
