using nPreCredit.Code;
using nPreCredit.DbModel;
using nPreCredit.DbModel.Repository;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api")]
    public partial class ApiUpdateDataWarehouseController : NController
    {
        [Route("DataWarehouse/Update")]
        [HttpPost]
        public ActionResult UpdateDataWarehouse()
        {
            var transactionDate = Clock.Today;
            if (NEnv.IsUnsecuredLoansEnabled)
            {
                MergeDates(new List<DateTime>() { transactionDate });

                Merge_Dimension_CreditApplication();

                Merge_Fact_CreditApplicationSnapshot(transactionDate);

                Merge_Fact_CreditApplicationCancellation(transactionDate);

                Merge_Dimension_CreditApplicationArchival();

                Merge_Fact_CurrentCreditDecisionEffectiveInterestRate(transactionDate);

                Merge_Fact_CreditApplicationLatestCreditDecision(transactionDate);

                Merge_Fact_CreditApplicationFinalDecision(transactionDate);
            }


            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        private void MergeDimension<T, U>(Func<PreCreditContext, IQueryable<T>> getBaseQuery, SystemItemCode code, string dimensionName, Func<List<T>, PreCreditContext, List<U>> toDwItems, int batchSize) where T : TimestampedItem
        {
            Merge(getBaseQuery, code, dimensionName, toDwItems, false, batchSize);
        }

        private void MergeFact<T, U>(Func<PreCreditContext, IQueryable<T>> getBaseQuery, SystemItemCode code, string factName, Func<List<T>, PreCreditContext, List<U>> toDwItems, int batchSize) where T : TimestampedItem
        {
            Merge(getBaseQuery, code, factName, toDwItems, true, batchSize);
        }

        private void MergeUsingIdentityInt<T, U>(Func<PreCreditContext, IQueryable<T>> getBaseQuery, SystemItemCode code, string dimOrFactName, Func<List<T>, PreCreditContext, List<U>> toDwItems, bool isFact, int batchSize) where T : IdentityIntItem
        {
            var systemItemRepo = new SystemItemRepository(this.CurrentUserId, this.InformationMetadata, this.Clock);

            int? latestSeenId;
            int? maxId;
            using (var context = new PreCreditContext())
            {
                latestSeenId = systemItemRepo.GetInt(code, context);

                var q = getBaseQuery(context);

                if (latestSeenId.HasValue)
                {
                    var lid = latestSeenId.Value;
                    q = q.Where(x => x.MergeId > lid);
                }

                maxId = q.OrderByDescending(x => x.MergeId).Select(x => (int?)x.MergeId).FirstOrDefault();
            }

            if (!maxId.HasValue)
                return;

            var client = new DataWarehouseClient();

            int count;
            do
            {
                using (var context = new PreCreditContext())
                {
                    var q = getBaseQuery(context);

                    if (latestSeenId.HasValue)
                    {
                        var lid = latestSeenId.Value;
                        q = q.Where(x => x.MergeId > lid);
                    }

                    var result = q.Where(x => x.MergeId <= maxId.Value)
                        .OrderBy(x => x.MergeId)
                        .Take(batchSize);

                    var newLatestSeenId = result.OrderByDescending(x => x.MergeId).Select(x => (int?)x.MergeId).FirstOrDefault();

                    var dimsOrFacts = toDwItems(result.ToList(), context);

                    count = dimsOrFacts.Count;
                    if (count > 0)
                    {
                        if (isFact)
                            client.MergeFact(dimOrFactName, dimsOrFacts);
                        else
                            client.MergeDimension(dimOrFactName, dimsOrFacts);
                    }

                    if (newLatestSeenId.HasValue && newLatestSeenId != latestSeenId)
                    {
                        systemItemRepo.SetInt(code, newLatestSeenId, context);
                    }

                    latestSeenId = newLatestSeenId;

                    context.SaveChanges();
                }
            }
            while (count > 0);
        }

        private void Merge<T, U>(Func<PreCreditContext, IQueryable<T>> getBaseQuery, SystemItemCode code, string dimOrFactName, Func<List<T>, PreCreditContext, List<U>> toDwItems, bool isFact, int batchSize) where T : TimestampedItem
        {
            var systemItemRepo = new SystemItemRepository(this.CurrentUserId, this.InformationMetadata, Clock);

            byte[] latestSeenTs;
            byte[] maxTs;
            using (var context = new PreCreditContext())
            {
                latestSeenTs = systemItemRepo.GetTimestamp(code, context);

                var q = getBaseQuery(context);

                if (latestSeenTs != null)
                    q = q.Where(x => BinaryComparer.Compare(x.Timestamp, latestSeenTs) > 0);

                maxTs = q.OrderByDescending(x => x.Timestamp).Select(x => x.Timestamp).FirstOrDefault();
            }

            if (maxTs == null)
                return;

            var client = new DataWarehouseClient();

            int count;
            do
            {
                using (var context = new PreCreditContext())
                {
                    var q = getBaseQuery(context);

                    if (latestSeenTs != null)
                        q = q.Where(x => BinaryComparer.Compare(x.Timestamp, latestSeenTs) > 0);

                    var result = q.Where(x => BinaryComparer.Compare(x.Timestamp, maxTs) <= 0)
                        .OrderBy(x => x.Timestamp)
                        .Take(batchSize);

                    var newLatestSeenTs = result.OrderByDescending(x => x.Timestamp).Select(x => x.Timestamp).FirstOrDefault();

                    var dimsOrFacts = toDwItems(result.ToList(), context);

                    count = dimsOrFacts.Count;
                    if (count > 0)
                    {
                        if (isFact)
                            client.MergeFact(dimOrFactName, dimsOrFacts);
                        else
                            client.MergeDimension(dimOrFactName, dimsOrFacts);
                    }

                    if (newLatestSeenTs != null && newLatestSeenTs != latestSeenTs)
                    {
                        //force single appears here since saving the result of doing this filtered will cause rows to be skipped if they appear out of order.
                        systemItemRepo.SetTimestamp(code, newLatestSeenTs, context);
                    }

                    latestSeenTs = newLatestSeenTs;

                    context.SaveChanges();
                }
            }
            while (count > 0);
        }

        //getBatch: (context, batchSize, latestSeenTs, globalMaxTs) => (Keys of next batch, max timestamp in the batch with those keys)
        private void MergeFastUsingIds<KeyType, ItemType>(
            Func<PreCreditContext, byte[]> getCurrentMaxTs, //To prevent endless replication in high write scenarios
            Func<PreCreditContext, int, byte[], byte[], Tuple<List<KeyType>, byte[]>> getBatch,
            SystemItemCode code, string dimOrFactName,
            Func<List<KeyType>, PreCreditContext, List<ItemType>> toDwItems,
            bool isFact,
            int batchSize)
        {
            var systemItemRepo = new SystemItemRepository(this.CurrentUserId, this.InformationMetadata, Clock);

            var client = new DataWarehouseClient();

            var timer = Stopwatch.StartNew();
            var maxTime = TimeSpan.FromHours(1);

            using (var context = new PreCreditContext())
            {
                var globalMaxTs = getCurrentMaxTs(context);
                var latestSeenTs = systemItemRepo.GetTimestamp(code, context);

                while (true)
                {
                    var b = getBatch(context, batchSize, latestSeenTs, globalMaxTs);
                    var keysInBatch = b.Item1;
                    var newLatestSeenTs = b.Item2;

                    if (newLatestSeenTs == null)
                        return;

                    if (keysInBatch.Count > 0)
                    {
                        var dataItemsInBatch = toDwItems(keysInBatch, context);
                        if (isFact)
                            client.MergeFact(dimOrFactName, dataItemsInBatch);
                        else
                            client.MergeDimension(dimOrFactName, dataItemsInBatch);
                    }

                    systemItemRepo.SetTimestamp(code, newLatestSeenTs, context);

                    if (latestSeenTs == newLatestSeenTs)
                    {
                        //This can happen when there are actually multiple timestamps represeting a single change (think head -> item relation with item timestamp used)
                        //In this case the low timestamp is used to to ensure we dont miss any changes to other items between min and max but it also means we can ge a final ghostbatch which keeps coming back. This prevents that from looping endlessly.
                        return;
                    }
                    context.SaveChanges();
                    latestSeenTs = newLatestSeenTs;

                    if (timer.Elapsed > maxTime)
                        throw new Exception("Replication of application snapshots is taking more than 1 hour. Aborting here. It can be restarted and will continue where it left of if this is not a looping bug.");
                }
            }
        }

        private void MergeDates(List<DateTime> dates)
        {
            var client = new DataWarehouseClient();

            client.MergeDimension("Date", dates.Select(x => new { Date = x }).ToList());
        }

        private int GetFullYearsBetween(DateTime d, DateTime d2)
        {
            if (d2 < d)
                return 0;

            var age = d2.Year - d.Year;

            return (d.AddYears(age + 1) <= d2) ? (age + 1) : age;
        }

        private static class BinaryComparer
        {
            public static int Compare(byte[] b1, byte[] b2)
            {
                throw new NotImplementedException();
            }
        }

        private class TimestampedItem
        {
            public byte[] Timestamp { get; internal set; }
        }

        private class IdentityIntItem
        {
            public int MergeId { get; set; }
        }
    }
}