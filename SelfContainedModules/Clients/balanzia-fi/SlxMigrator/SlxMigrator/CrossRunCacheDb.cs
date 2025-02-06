using Dapper;
using NTech.Banking.LoanModel;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlxMigrator
{
    /// <summary>
    /// This database lives in root and contains data that is stable over time and can be preserved between runs
    /// An example is initial effective interest rate for a loan
    /// 
    /// New loans can be added in later runs but already stored values can never change so they
    /// dont need to be recomputed.
    /// 
    /// Only delete this after fixing logic bugs
    /// 
    /// </summary>
    internal class CrossRunCacheDb : IDisposable
    {
        private SQLiteConnection connection;
        private readonly ConnectionFactory connectionFactory;

        public CrossRunCacheDb(string rootDir, ConnectionFactory connectionFactory)
        {
            connection = new SQLiteConnection($"Data Source={Path.Combine(rootDir, "crossRunCache.db")};Version=3");
            connection.Open();
            this.connectionFactory = connectionFactory;
        }

        public Dictionary<string, decimal?> GetInitialEffectiveInterestRatesForCredits(HashSet<string> creditNrs)
        {
            return connection
                .Query<EffOutData>("select c.CreditNr, c.Rate from LoanInitialEffectiveInterestRate c where c.CreditNr in @creditNrs", param: new { creditNrs })
                .ToDictionary(x => x.CreditNr, x => x.Rate);
        }
        
        public void WithConnection(Action<SQLiteConnection> a)
        {
            a(connection);
        }

        private class EffOutData
        {
            public string CreditNr { get; set; }
            public decimal? Rate { get; set; }
        }

        public void EnsureCreditInitialEffectiveInterestRate()
        {
            connection.Execute("create table if not exists LoanInitialEffectiveInterestRate (CreditNr TEXT NOT NULL PRIMARY KEY, CreatedByBusinessEventId INT NOT NULL, Rate NUMBER)");

            var lastCachedEventdId = connection.ExecuteScalar<int?>("select max(CreatedByBusinessEventId) from LoanInitialEffectiveInterestRate") ?? 0;
            using (var creditConnection = connectionFactory.CreateOpenConnection(DatabaseCode.Credit))
            {
                var lastSystemEventId = creditConnection.ExecuteScalar<int>("select max(h.CreatedByBusinessEventId) from CreditHeader h");
                if(lastSystemEventId <= lastCachedEventdId)
                {
                    return;
                }
                Console.WriteLine("Building the effective interest cache. This is a one time cross run operation that could take some time");
                while(true)
                {
                    var credits = creditConnection.Query<EffRateData>(
@"select top 200 h.CreditNr,
		((select top 1 d.[Value] from DatedCreditValue d where d.[Name] = 'MarginInterestRate' and d.BusinessEventId = h.CreatedByBusinessEventId)
		+ (select top 1 isnull(d.[Value], 0) from DatedCreditValue d where d.[Name] = 'ReferenceInterestRate' and d.BusinessEventId = h.CreatedByBusinessEventId))  as InitialInterestRate,
		(select top 1 d.[Value] from DatedCreditValue d where d.[Name] = 'AnnuityAmount' and d.BusinessEventId = h.CreatedByBusinessEventId) as InitialAnnuityAmount,
		(select sum(a.Amount) from AccountTransaction a join BusinessEvent b on b.Id = a.BusinessEventId  where a.AccountCode = 'CapitalDebt' and a.CreditNr = h.CreditNr and b.EventType in('NewCredit')) as InitialCapitalDebt,
		(select isnull(sum(a.Amount), 0) from AccountTransaction a join BusinessEvent b on b.Id = a.BusinessEventId  where a.AccountCode = 'CapitalDebt' and a.CreditNr = h.CreditNr and b.EventType in('CapitalizedInitialFee')) as InitialFeeAmount,
		(select top 1 isnull(d.[Value], 0) from DatedCreditValue d where d.[Name] = 'NotificationFee' and d.BusinessEventId = h.CreatedByBusinessEventId) as InitialNotificationFee,
		h.CreatedByBusinessEventId
from	CreditHeader h
where   h.CreatedByBusinessEventId > @lastCachedEventdId
order by h.CreatedByBusinessEventId asc", new { lastCachedEventdId }).ToList();

                    if (credits.Count == 0)
                        return;

                    foreach(var c in credits)
                    {
                        try
                        {

                            var paymentPlan = PaymentPlanCalculation
                                .BeginCreateWithAnnuity(c.InitialCapitalDebt, c.InitialAnnuityAmount, c.InitialInterestRate, null, false)
                                .WithInitialFeeCapitalized(c.InitialFeeAmount)
                                .WithMonthlyFee(c.InitialNotificationFee)
                                .EndCreate();

                            connection.Execute(
                                @"insert into LoanInitialEffectiveInterestRate (CreditNr, CreatedByBusinessEventId, Rate)
                               values (@CreditNr,@CreatedByBusinessEventId, @EffectiveInterestRatePercent)", param: new
                                {
                                    c.CreditNr,
                                    c.CreatedByBusinessEventId,
                                    paymentPlan.EffectiveInterestRatePercent
                                });
                        }
                        catch(Exception ex)
                        {
                            throw new Exception($"Effective interest rate failed for {c.CreditNr}", ex);
                        }
                    }

                    lastCachedEventdId = credits.Max(x => x.CreatedByBusinessEventId);
                }
            }
        }

        private class EffRateData
        {
            public string CreditNr { get; set; }
            public decimal InitialInterestRate { get; set; }
            public decimal InitialAnnuityAmount { get; set; }
            public decimal InitialCapitalDebt { get; set; }
            public decimal InitialFeeAmount { get; set; }
            public decimal InitialNotificationFee { get;  set; }
            public int CreatedByBusinessEventId { get; set; }
        }

        public void Dispose()
        {
            connection.Close();
            connection.Dispose();
        }
    }
}
