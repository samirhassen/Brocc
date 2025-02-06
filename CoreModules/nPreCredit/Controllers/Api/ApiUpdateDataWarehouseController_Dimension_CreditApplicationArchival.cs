using nPreCredit.DbModel;
using System;
using System.Linq;

namespace nPreCredit.Controllers.Api
{
    public partial class ApiUpdateDataWarehouseController
    {
        private void Merge_Dimension_CreditApplicationArchival()
        {
            const string DimensionName = "CreditApplicationArchival";
            MergeDimension(
                x => CreditApplicationArchivalQuery(x),
                SystemItemCode.DwLatestMergedTimestamp_Dimension_CreditApplicationArchival,
                DimensionName,
                (items, context) =>
                {
                    return items.Select(x => new
                    {
                        x.ApplicationNr,
                        ArchivedLevel = x.ArchivedLevel,
                        x.ArchivedDate
                    }).ToList();
                }, 300);
        }

        private class CreditApplicationArchivalModel : TimestampedItem
        {
            public string ApplicationNr { get; set; }
            public int? ArchivedLevel { get; set; }
            public DateTimeOffset? ArchivedDate { get; set; }
        }

        private IQueryable<CreditApplicationArchivalModel> CreditApplicationArchivalQuery(PreCreditContext context)
        {
            var q = context
                .CreditApplicationHeaders
                .Where(x => x.ArchivedDate.HasValue)
                .AsQueryable();

            return q
                .Select(x => new CreditApplicationArchivalModel
                {
                    ApplicationNr = x.ApplicationNr,
                    ArchivedDate = x.ArchivedDate,
                    ArchivedLevel = x.ArchiveLevel,
                    Timestamp = x.Timestamp
                });
        }
    }
}