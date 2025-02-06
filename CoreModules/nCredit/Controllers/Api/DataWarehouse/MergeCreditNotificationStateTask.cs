using nCredit.Code;
using nCredit.DbModel.Repository;
using NTech;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Linq;

namespace nCredit.Controllers.Api.DataWarehouse
{
    public class MergeCreditNotificationStateTask : DatawarehouseMergeTask
    {
        public override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled || NEnv.IsCompanyLoansEnabled;

        public override void Merge(INTechCurrentUserMetadata currentUser, IClock clock)
        {
            int count = 0;
            while (count++ < 10000)
            {
                if (!MergeI(currentUser))
                    return;
            }
            throw new Exception("Hit guard code in Merge_Fact_CreditNotificationState");
        }

        private bool MergeI(INTechCurrentUserMetadata currentUser)
        {
            var repo = new SystemItemRepository(currentUser.UserId, currentUser.InformationMetadata);

            byte[] maxTs;
            byte[] latestSeenTs;
            using (var context = new CreditContext())
            {
                latestSeenTs = repo.GetTimestamp(SystemItemCode.DwLatestMergedTimestamp_Fact_CreditNotificationState, context);

                var q = context
                    .CreditNotificationHeaders
                    .Select(x => new
                    {
                        x.Id,
                        x.CreditNr,
                        x.DueDate,
                        x.ClosedTransactionDate,
                        x.Timestamp
                    });

                if (latestSeenTs != null)
                    q = q.Where(x => BinaryComparer.Compare(x.Timestamp, latestSeenTs) > 0);

                var batchData = q.OrderBy(x => x.Timestamp).Take(1000).Select(x => new
                {
                    x.Timestamp,
                    Item = new CreditNotificationStateModel
                    {
                        ClosedDate = x.ClosedTransactionDate,
                        CreditNr = x.CreditNr,
                        DueDate = x.DueDate,
                        NotificationId = x.Id
                    }
                });

                maxTs = batchData.OrderByDescending(x => x.Timestamp).Select(x => x.Timestamp).FirstOrDefault();
                if (maxTs == null)
                    return false;

                var items = batchData.Select(x => x.Item).ToList();
                foreach (var i in items)
                {
                    i.DueMonth = new DateTime(i.DueDate.Year, i.DueDate.Month, 1);
                    i.IsOpen = !i.ClosedDate.HasValue;
                    i.DwUpdatedDate = DateTime.Now; //Intentionally not Clock.Today
                }

                var client = new DataWarehouseClient();
                client.MergeFact("CreditNotificationState", items);

                repo.SetTimestamp(SystemItemCode.DwLatestMergedTimestamp_Fact_CreditNotificationState, maxTs, context);

                context.SaveChanges();

                return true;
            }
        }

        private class CreditNotificationStateModel
        {
            public int NotificationId { get; set; }
            public DateTime Date { get; set; }
            public string CreditNr { get; set; }
            public DateTime DueMonth { get; set; }
            public DateTime DueDate { get; set; }
            public bool IsOpen { get; set; }
            public DateTime? ClosedDate { get; set; }
            public DateTime DwUpdatedDate { get; set; }
        }
    }
}