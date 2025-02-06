using Dapper;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Credit.Shared.Models;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services.SwedishMortgageLoans
{
    public class AnnexTwelveEsmaReportService
    {
        private readonly CreditContextFactory creditContextFactory;

        private readonly IClientConfigurationCore clientConfiguration;

        public AnnexTwelveEsmaReportService(CreditContextFactory creditContextFactory,
            IClientConfigurationCore clientConfiguration)
        {
            this.creditContextFactory = creditContextFactory;
            this.clientConfiguration = clientConfiguration;
        }

        private static Lazy<string> Query = new Lazy<string>(() =>
        {
            string GetInPeriodRecoveredXAmount(string accountCode) =>
@"(select -isnull(sum(p.Amount), 0)
 from    AccountTransactionPeriodEnd p 
 where   p.CreditNr = c.CreditNr 
 and     p.HasActiveOverdueTerminationLetter = 1
 and     p.IncomingPaymentId is not null 
 and     p.AccountCode = '[[ACCOUNT_CODE]]'
 and     p.TransactionDate >= @fromDate)"
    .Replace(@"[[ACCOUNT_CODE]]", accountCode)
    .Replace(Environment.NewLine, " ");

            var t = new EsmaAnnexTwelveLoan { };
            var d = new Dictionary<string, string>
            {
                [nameof(t.CreditNr)] = "c.CreditNr",
                [nameof(t.InPeriodRecoveredCapitalDebtAmount)] = GetInPeriodRecoveredXAmount("CapitalDebt"),
                [nameof(t.InPeriodRecoveredInterestDebtAmount)] = GetInPeriodRecoveredXAmount("InterestDebt"),
            };
            var nl = Environment.NewLine;
            return "select " +  string.Join($", {nl}", d.Select(x => $"{x.Value} as [{x.Key}]")) + $"{nl}from DimensionCredit c";
        });      

        public EsmaAnnexTwelveReportResponse GetAnnexTwelveReportData(FromDateToDateReportRequest request, Action<string> observeSqlQuery = null)
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
                var loans = CommonReportingTableExpressions.Query<EsmaAnnexTwelveLoan>(context.GetConnection(), Query.Value, toDate, graceDays,
                    addExtraParameters: x => x["fromDate"] = fromDate,
                    observeSqlQuery: observeSqlQuery);

                return new EsmaAnnexTwelveReportResponse
                {
                    Loans = loans
                };                
            }            
        }
    }

    public class EsmaAnnexTwelveReportResponse
    {
        public List<EsmaAnnexTwelveLoan> Loans { get; set; }
    }

    public class EsmaAnnexTwelveLoan
    {
        /// <summary>
        /// Credit nr/loan nr
        /// </summary>
        public string CreditNr { get; set; }

        /// <summary>
        /// Paid capital/principal during the period on loans that had and overdue termination letter when the payment was made.
        /// IVSS14 Principal Recoveries In The Period
        /// </summary>
        public decimal InPeriodRecoveredCapitalDebtAmount { get; set; }

        /// <summary>
        /// Paid interest during the period on loans that had and overdue termination letter when the payment was made.
        /// IVSS15 Interest Recoveries In The Period
        /// </summary>
        public decimal InPeriodRecoveredInterestDebtAmount { get; set; }
    }
}
