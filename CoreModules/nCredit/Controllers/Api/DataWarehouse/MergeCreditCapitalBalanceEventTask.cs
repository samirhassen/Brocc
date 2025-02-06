using nCredit.Code;
using nCredit.DbModel.Repository;
using NTech;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Linq;

namespace nCredit.Controllers.Api.DataWarehouse
{
    public class MergeCreditCapitalBalanceEventTask : DatawarehouseMergeTask
    {
        public override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled || NEnv.IsCompanyLoansEnabled;

        public override void Merge(INTechCurrentUserMetadata currentUser, IClock clock)
        {
            var repo = new SystemItemRepository(currentUser.UserId, currentUser.InformationMetadata);

            Func<CreditContext, IQueryable<CreditBalanceEventModel>> getQuery = c =>
            {
                return c.Transactions.Where(x => x.CreditNr != null && x.AccountCode == TransactionAccountType.CapitalDebt.ToString()).Select(x => new CreditBalanceEventModel
                {
                    CreditNr = x.CreditNr,
                    Amount = x.Amount,
                    EventType = x.BusinessEvent.EventType,
                    Timestamp = x.Timestamp,
                    TransactionDate = x.TransactionDate,
                    TransactionId = x.Id
                });
            };

            byte[] maxTs;
            byte[] latestSeenTs;
            using (var context = new CreditContext())
            {
                latestSeenTs = repo.GetTimestamp(SystemItemCode.DwLatestMergedTimestamp_Fact_CreditBalanceEvent, context);

                var q = getQuery(context);
                if (latestSeenTs != null)
                    q = q.Where(x => BinaryComparer.Compare(x.Timestamp, latestSeenTs) > 0);

                maxTs = q.OrderByDescending(x => x.Timestamp).Select(x => x.Timestamp).FirstOrDefault();
            }

            var client = new DataWarehouseClient();

            var date = DateTime.Today;
            if (maxTs != null)
            {
                int count;
                do
                {
                    using (var context = new CreditContext())
                    {
                        var q = getQuery(context).Where(x => BinaryComparer.Compare(x.Timestamp, maxTs) <= 0);
                        if (latestSeenTs != null)
                            q = q.Where(x => BinaryComparer.Compare(x.Timestamp, latestSeenTs) > 0);
                        var result = q.OrderBy(x => x.Timestamp).Take(500).ToList();
                        count = result.Count;
                        if (result.Count > 0)
                        {
                            var actualFacts = result.Select(x => new
                            {
                                x.CreditNr,
                                x.Amount,
                                x.EventType,
                                x.TransactionDate,
                                x.TransactionId
                            }).ToList();
                            if (actualFacts.Count > 0)
                            {
                                client.MergeDimension("Date", actualFacts.Select(x => x.TransactionDate).Distinct().Select(x => new { Date = x }).ToList());
                                client.MergeFact("CreditCapitalBalanceEvent", actualFacts);
                            }
                            latestSeenTs = result.Last().Timestamp;
                            repo.SetTimestamp(SystemItemCode.DwLatestMergedTimestamp_Fact_CreditBalanceEvent, latestSeenTs, context);
                            context.SaveChanges();
                        }
                    }
                }
                while (count > 0);
            }
        }

        private class CreditBalanceEventModel
        {
            public long TransactionId { get; set; }
            public string CreditNr { get; set; }
            public decimal Amount { get; set; }
            public DateTime TransactionDate { get; set; }
            public string EventType { get; set; }
            public byte[] Timestamp { get; set; }
        }

    }
}