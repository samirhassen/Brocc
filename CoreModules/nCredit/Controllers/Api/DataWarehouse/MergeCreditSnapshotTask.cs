using nCredit.Code;
using nCredit.DbModel.Repository;
using NTech;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Linq;

namespace nCredit.Controllers.Api.DataWarehouse
{
    public class MergeCreditSnapshotTask : DatawarehouseMergeTask
    {
        public override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled;

        public override void Merge(INTechCurrentUserMetadata currentUser, IClock clock)
        {
            var repo = new SystemItemRepository(currentUser.UserId, currentUser.InformationMetadata);

            Func<CreditContext, IQueryable<CreditSnapshotModel>> getQuery = c =>
            {
                return c.CreditHeaders.Select(x => new
                {
                    x.CreditNr,
                    LatestTimestamp = x
                                        .Transactions.Select(y => y.Timestamp)
                                        .Concat(new[] { x.Timestamp })
                                        .Concat(x.DatedCreditDates.Select(y => y.Timestamp))
                                        .Concat(x.DatedCreditValues.Select(y => y.Timestamp))
                                        .Concat(x.DatedCreditStrings.Select(y => y.Timestamp))
                                        .Max(),

                    StatusItem = x.DatedCreditStrings.Where(y => y.Name == DatedCreditStringCode.CreditStatus.ToString()).OrderByDescending(y => y.TransactionDate).ThenByDescending(y => y.Timestamp).FirstOrDefault(),
                    MarginInterestRate = x.DatedCreditValues.Where(y => y.Name == DatedCreditValueCode.MarginInterestRate.ToString()).OrderByDescending(y => y.TransactionDate).ThenByDescending(y => y.Timestamp).Select(y => (decimal?)y.Value).FirstOrDefault() ?? 0m,
                    ReferenceInterestRate = x.DatedCreditValues.Where(y => y.Name == DatedCreditValueCode.ReferenceInterestRate.ToString()).OrderByDescending(y => y.TransactionDate).ThenByDescending(y => y.Timestamp).Select(y => (decimal?)y.Value).FirstOrDefault() ?? 0m,
                    NotificationFee = x.DatedCreditValues.Where(y => y.Name == DatedCreditValueCode.NotificationFee.ToString()).OrderByDescending(y => y.TransactionDate).ThenByDescending(y => y.Timestamp).Select(y => (decimal?)y.Value).FirstOrDefault() ?? 0m,
                    AnnuityAmount = x.DatedCreditValues.Where(y => y.Name == DatedCreditValueCode.AnnuityAmount.ToString()).OrderByDescending(y => y.TransactionDate).ThenByDescending(y => y.Timestamp).Select(y => (decimal?)y.Value).FirstOrDefault(),
                    CapitalBalance = x.Transactions.Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m
                }).Select(x => new CreditSnapshotModel
                {
                    AnnuityAmount = x.AnnuityAmount,
                    CreditNr = x.CreditNr,
                    CapitalBalance = x.CapitalBalance,
                    MarginInterestRate = x.MarginInterestRate,
                    TotalInterestRate = x.MarginInterestRate + x.ReferenceInterestRate,
                    NotificationFee = x.NotificationFee,
                    Status = x.StatusItem.Value,
                    StatusDate = x.StatusItem.TransactionDate,
                    Timestamp = x.LatestTimestamp
                });
            };

            byte[] maxTs;
            byte[] latestSeenTs;
            using (var context = new CreditContext())
            {
                latestSeenTs = repo.GetTimestamp(SystemItemCode.DwLatestMergedTimestamp_Fact_CreditSnapshot, context);

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
                                Date = date,
                                x.AnnuityAmount,
                                x.CapitalBalance,
                                x.MarginInterestRate,
                                x.TotalInterestRate,
                                x.NotificationFee,
                                x.Status,
                                x.StatusDate
                            }).ToList();
                            if (actualFacts.Count > 0)
                            {
                                client.MergeDimension("Date", actualFacts.Select(x => x.Date).Distinct().Select(x => new { Date = x }).ToList());
                                client.MergeFact("CreditSnapshot", actualFacts);
                            }
                            latestSeenTs = result.Last().Timestamp;
                            repo.SetTimestamp(SystemItemCode.DwLatestMergedTimestamp_Fact_CreditSnapshot, latestSeenTs, context);
                            context.SaveChanges();
                        }
                    }
                }
                while (count > 0);
            }
        }

        private class CreditSnapshotModel
        {
            public string CreditNr { get; set; }
            public string Status { get; set; }
            public DateTime? StatusDate { get; set; }
            public decimal CapitalBalance { get; set; }
            public decimal MarginInterestRate { get; set; }
            public decimal TotalInterestRate { get; set; }
            public decimal NotificationFee { get; set; }
            public decimal? AnnuityAmount { get; set; }
            public byte[] Timestamp { get; set; }
        }
    }
}