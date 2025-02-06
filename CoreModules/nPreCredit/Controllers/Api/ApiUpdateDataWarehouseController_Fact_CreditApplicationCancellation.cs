using nPreCredit.DbModel;
using System;
using System.Linq;

namespace nPreCredit.Controllers.Api
{
    public partial class ApiUpdateDataWarehouseController
    {
        private void Merge_Fact_CreditApplicationCancellation(DateTime transactionDate)
        {
            const string FactName = "CreditApplicationCancellation";
            MergeFact(
                x => CreditApplicationCancellationQuery(x),
                SystemItemCode.DwLatestMergedTimestamp_Fact_CreditApplicationCancellation,
                FactName,
                (items, context) =>
                {
                    return items.Select(x => new
                    {
                        Date = transactionDate,
                        ApplicationNr = x.ApplicationNr,
                        CancelledDate = x.CancelledDate.Value.Date,
                        CancelledBy = x.CancelledBy,
                        CancelledState = x.CancelledState
                    }).ToList();
                }, 300);
        }

        private class CreditApplicationCancellationFactModel : TimestampedItem
        {
            public string ApplicationNr { get; set; }
            public string CancelledState { get; set; }
            public DateTimeOffset? CancelledDate { get; set; }
            public int? CancelledBy { get; set; }
        }

        private IQueryable<CreditApplicationCancellationFactModel> CreditApplicationCancellationQuery(PreCreditContext context)
        {
            var q = context
                .CreditApplicationHeaders
                .Where(x => !x.ArchivedDate.HasValue && x.CancelledDate.HasValue);

            return q
                .Select(x => new CreditApplicationCancellationFactModel
                {
                    ApplicationNr = x.ApplicationNr,
                    CancelledBy = x.CancelledBy,
                    CancelledDate = x.CancelledDate,
                    CancelledState = x.CancelledState,
                    Timestamp = x.Timestamp
                });
        }
    }
}