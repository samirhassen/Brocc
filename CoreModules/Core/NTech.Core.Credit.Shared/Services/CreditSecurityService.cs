using NTech.Core.Credit.Shared.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.Code.Services
{
    public class CreditSecurityService : ICreditSecurityService
    {
        private readonly CreditContextFactory contextFactory;

        public CreditSecurityService(CreditContextFactory contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        private IQueryable<CreditSecurityItem> GetCurrentItemsModel(ICreditContextExtended context, int? lastIncludedBusinessEventId)
        {
            var pre = context
                .CreditSecurityItemsQueryable;
            if (lastIncludedBusinessEventId.HasValue)
                pre = pre.Where(x => x.CreatedByBusinessEventId <= lastIncludedBusinessEventId.Value);

            return pre
                .GroupBy(x => new { x.CreditNr, x.Name })
                .Select(x => x.OrderByDescending(y => y.CreatedByBusinessEventId).FirstOrDefault());
        }


        public List<CreditSecurityItemModel> FetchSecurityItems(string creditNr, int? lastIncludedBusinessEventId = null)
        {
            using (var context = contextFactory.CreateContext())
            {
                return GetCurrentItemsModel(context, lastIncludedBusinessEventId)
                    .Where(x => x.CreditNr == creditNr)
                    .Select(x => new CreditSecurityItemModel
                    {
                        CreditNr = x.CreditNr,
                        DateValue = x.DateValue,
                        Id = x.Id,
                        Name = x.Name,
                        NumericValue = x.NumericValue,
                        StringValue = x.StringValue,
                        TransactionDate = x.CreatedByEvent.TransactionDate
                    })
                    .ToList();
            }
        }
    }

    public interface ICreditSecurityService
    {
        List<CreditSecurityItemModel> FetchSecurityItems(string creditNr, int? lastIncludedBusinessEventId = null);
    }

    public class CreditSecurityItemModel
    {
        public int Id { get; set; }
        public string CreditNr { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Name { get; set; }
        public string StringValue { get; set; }
        public decimal? NumericValue { get; set; }
        public DateTime? DateValue { get; set; }
    }
}