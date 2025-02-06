using NTech.Core.Module.Shared.Database;
using System;
using System.Linq;

namespace nCredit
{
    public class CollateralItem : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public CollateralHeader Collateral { get; set; }
        public int CollateralHeaderId { get; set; }
        public string ItemName { get; set; }
        public string StringValue { get; set; }
        public decimal? NumericValue { get; set; }
        public DateTime? DateValue { get; set; }
        public int CreatedByBusinessEventId { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
        public int? RemovedByBusinessEventId { get; set; }
        public BusinessEvent RemovedByEvent { get; set; }

        public static IQueryable<CollateralItem> GetLatestCollateralItems(IQueryable<CollateralItem> query)
        {
            return query
                .Where(x => !x.RemovedByBusinessEventId.HasValue)
                .GroupBy(x => new { x.CollateralHeaderId, x.ItemName })
                .Select(x => x.OrderByDescending(y => y.CreatedByBusinessEventId).FirstOrDefault());
        }
    }
}