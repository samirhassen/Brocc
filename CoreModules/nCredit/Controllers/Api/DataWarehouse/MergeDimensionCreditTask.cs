using nCredit.Code;
using nCredit.DbModel.Repository;
using NTech;
using NTech.Core.Module.Shared.Infrastructure;
using System.Linq;

namespace nCredit.Controllers.Api.DataWarehouse
{
    public class MergeDimensionCreditTask : DatawarehouseMergeTask
    {
        public override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled || NEnv.IsCompanyLoansEnabled;

        public override void Merge(INTechCurrentUserMetadata currentUser, IClock clock)
        {
            var repo = new SystemItemRepository(currentUser.UserId, currentUser.InformationMetadata);

            byte[] maxTs;
            byte[] latestSeenTs;
            using (var context = new CreditContext())
            {
                latestSeenTs = repo.GetTimestamp(SystemItemCode.DwLatestMergedTimestamp_Dimension_Credit, context);

                var q = context.CreditHeaders.AsQueryable();
                if (latestSeenTs != null)
                    q = q.Where(x => BinaryComparer.Compare(x.Timestamp, latestSeenTs) > 0);

                maxTs = q.OrderByDescending(x => x.Timestamp).Select(x => x.Timestamp).FirstOrDefault();
            }

            var client = new DataWarehouseClient();

            if (maxTs != null)
            {
                int count;
                do
                {
                    using (var context = new CreditContext())
                    {
                        var q = context.CreditHeaders.Where(x => BinaryComparer.Compare(x.Timestamp, maxTs) <= 0);
                        if (latestSeenTs != null)
                            q = q.Where(x => BinaryComparer.Compare(x.Timestamp, latestSeenTs) > 0);
                        var result = q.OrderBy(x => x.Timestamp).Select(x => new { x.CreditNr, x.ProviderName, x.NrOfApplicants, x.StartDate, x.Timestamp }).Take(500).ToList();
                        count = result.Count;
                        if (result.Count > 0)
                        {
                            client.MergeDimension("Credit", result.Select(x => new { x.CreditNr, x.ProviderName, x.NrOfApplicants, x.StartDate }).ToList());
                            latestSeenTs = result.Last().Timestamp;
                            repo.SetTimestamp(SystemItemCode.DwLatestMergedTimestamp_Dimension_Credit, latestSeenTs, context);
                            context.SaveChanges();
                        }
                    }
                }
                while (count > 0);
            }
        }
    }
}