using Dapper;
using Newtonsoft.Json;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCustomer.Code.Services.Kyc
{
    public class KycScreeningManagementService : IKycScreeningManagementService
    {
        private readonly CustomerContextFactory contextFactory;
        private readonly EncryptionService encryptionService;

        public KycScreeningManagementService(CustomerContextFactory contextFactory, EncryptionService encryptionService)
        {
            this.contextFactory = contextFactory;
            this.encryptionService = encryptionService;
        }

        public KycScreeningQueryResultModel FetchLatestQueryResult(int customerId)
        {
            using (var context = contextFactory.CreateContext())
            {
                return context
                    .TrapetsQueryResultsQueryable
                    .Where(x => x.CustomerId == customerId)
                    .OrderByDescending(x => x.Id)
                    .ThenByDescending(x => x.QueryDate)
                    .Select(x => new KycScreeningQueryResultModel
                    {
                        CustomerId = x.CustomerId,
                        Id = x.Id,
                        IsPepHit = x.IsPepHit,
                        IsSanctionHit = x.IsSanctionHit,
                        QueryDate = x.QueryDate
                    })
                    .FirstOrDefault();
            }
        }

        public KycResultDetailsResponse GetKycResultDetails(KycResultDetailsRequest request)
        {
            using (var context = contextFactory.CreateContext())
            {
                var itemNames = new List<string> { "SanctionHitExternalIds", "PepHitExternalIds" };
                var result = context
                    .TrapetsQueryResultItemsQueryable
                    .Where(x => x.TrapetsQueryResultId == request.QueryId && itemNames.Contains(x.Name))
                    .Select(x => new { x.Name, x.IsEncrypted, x.Value })
                    .ToList();

                IDictionary<long, string> decryptedValues = null;
                if (result.Any(x => x.IsEncrypted))
                {
                    decryptedValues = encryptionService.DecryptEncryptedValues(context, result.Select(x => long.Parse(x.Value)).ToArray());
                }

                List<string> ParseValue(string name)
                {
                    var item = result.SingleOrDefault(x => x.Name == name);
                    if (item == null)
                        return new List<string>();
                    var rawValue = item.IsEncrypted ? decryptedValues.Req(long.Parse(item.Value)) : item.Value;
                    return JsonConvert.DeserializeObject<List<string>>(rawValue) ?? new List<string>();
                }

                return new KycResultDetailsResponse
                {
                    PepExternalIds = ParseValue("PepHitExternalIds"),
                    SactionExternalIds = ParseValue("SanctionHitExternalIds")
                };
            }
        }

        public GetKycResultDetailsHistoryResponse GetKycResultDetailsHistory(GetKycResultDetailsHistoryRequest request)
        {
            using (var context = contextFactory.CreateContext())
            {
                var fromDate = context.CoreClock.Today.AddDays(-request.HistoryDayCount);
                var toDate = context.CoreClock.Today;

                var itemNames = new List<string> { "SanctionHitExternalIds", "PepHitExternalIds" };
                var result = context
                    .TrapetsQueryResultItemsQueryable
                    .Where(x => x.QueryResult.CustomerId == request.CustomerId && x.QueryResult.QueryDate >= fromDate && x.QueryResult.QueryDate <= toDate && itemNames.Contains(x.Name))
                    .Select(x => new { x.QueryResult.QueryDate, x.QueryResult.Id, x.IsEncrypted, x.Name, x.Value })
                    .ToList();

                IDictionary<long, string> decryptedValues = null;
                if (result.Any(x => x.IsEncrypted))
                {
                    decryptedValues = encryptionService.DecryptEncryptedValues(context, result.Select(x => long.Parse(x.Value)).ToArray());
                }


                var resultPerDate = result
                    .GroupBy(x => x.QueryDate)
                    .ToDictionary(x => x.Key, queryItemForDate =>
                    {
                        var pepExternalIds = new HashSet<string>();
                        var sanctionExternalIds = new HashSet<string>();
                        foreach (var queryItem in queryItemForDate)
                        {
                            var rawValue = queryItem.IsEncrypted ? decryptedValues.Req(long.Parse(queryItem.Value)) : queryItem.Value;
                            var ids = JsonConvert.DeserializeObject<List<string>>(rawValue) ?? new List<string>();
                            (queryItem.Name == "PepHitExternalIds" ? pepExternalIds : sanctionExternalIds).AddRange(ids);
                        }
                        return (PepExternalIds: pepExternalIds.ToList(), SanctionExternalIds: sanctionExternalIds.ToList());
                    });

                return new GetKycResultDetailsHistoryResponse
                {
                    QueryDatesWithListHits = resultPerDate
                    .Keys
                    .OrderByDescending(x => x)
                    .Select(x => new GetKycResultDetailsHistoryResponseDate
                    {
                        QueryDate = x,
                        PepExternalIds = resultPerDate[x].PepExternalIds,
                        SanctionExternalIds = resultPerDate[x].SanctionExternalIds
                    })
                    .ToList()
                };
            }
        }

        private class KycScreeningTmp
        {
            public DateTime QueryDate { get; set; }
            public int IsPepHit { get; set; }
            public int IsSanctionHit { get; set; }

            public bool GetIsPepHit() { return IsPepHit > 0; }
            public bool GetIsSanctionHit() { return IsSanctionHit > 0; }
        }
        public KycScreeningQueryHistorySummaryModel FetchQueryHistorySummary(int customerId)
        {
            using (var context = contextFactory.CreateContext())
            {
                var rawItems = context.GetConnection().Query<KycScreeningTmp>(
@"select	QueryDate, 
		max(cast(t.IsPepHit as int)) as IsPepHit, 
		max(cast(t.IsSanctionHit as int)) as IsSanctionHit
from	TrapetsQueryResult t
where	CustomerId = @customerId
group by t.QueryDate", param: new { customerId }).ToList(); //Raw sql since all linq variations I could come up with did really dumb stuff

                var pepItems = new List<KycScreeningQueryHistorySummaryItem>();
                var sanctionItems = new List<KycScreeningQueryHistorySummaryItem>();
                KycScreeningQueryHistorySummaryItem pepItem = null;
                KycScreeningQueryHistorySummaryItem sanctionItem = null;
                foreach (var r in rawItems.OrderBy(x => x.QueryDate))
                {
                    if (pepItem?.Value != r.GetIsPepHit())
                    {
                        if (pepItem != null)
                            pepItems.Add(pepItem);
                        pepItem = new KycScreeningQueryHistorySummaryItem
                        {
                            Count = 1,
                            FromDate = r.QueryDate,
                            ToDate = r.QueryDate,
                            Value = r.GetIsPepHit()
                        };
                    }
                    else
                    {
                        pepItem.Count = pepItem.Count + 1;
                        pepItem.ToDate = r.QueryDate;
                    }

                    if (sanctionItem?.Value != r.GetIsSanctionHit())
                    {
                        if (sanctionItem != null)
                            sanctionItems.Add(sanctionItem);
                        sanctionItem = new KycScreeningQueryHistorySummaryItem
                        {
                            Count = 1,
                            FromDate = r.QueryDate,
                            ToDate = r.QueryDate,
                            Value = r.GetIsSanctionHit()
                        };
                    }
                    else
                    {
                        sanctionItem.Count = sanctionItem.Count + 1;
                        sanctionItem.ToDate = r.QueryDate;
                    }
                }
                if (pepItem != null)
                    pepItems.Add(pepItem);
                if (sanctionItem != null)
                    sanctionItems.Add(sanctionItem);

                pepItems.Reverse();
                sanctionItems.Reverse();

                return new KycScreeningQueryHistorySummaryModel
                {
                    CustomerId = customerId,
                    PepItems = pepItems,
                    SanctionItems = sanctionItems
                };
            }
        }
    }

    public interface IKycScreeningManagementService
    {
        KycScreeningQueryResultModel FetchLatestQueryResult(int customerId);
        KycScreeningQueryHistorySummaryModel FetchQueryHistorySummary(int customerId);
        KycResultDetailsResponse GetKycResultDetails(KycResultDetailsRequest request);
        GetKycResultDetailsHistoryResponse GetKycResultDetailsHistory(GetKycResultDetailsHistoryRequest request);
    }

    public class KycScreeningQueryHistorySummaryModel
    {
        public int CustomerId { get; set; }
        public List<KycScreeningQueryHistorySummaryItem> PepItems { get; set; }
        public List<KycScreeningQueryHistorySummaryItem> SanctionItems { get; set; }
    }

    public class KycScreeningQueryHistorySummaryItem
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int Count { get; set; } //NOTE: Can be fewer than To - From since there can be days when no screening was done
        public bool Value { get; set; }
    }

    public class KycScreeningQueryResultModel
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public DateTime QueryDate { get; set; }
        public bool IsPepHit { get; set; }
        public bool IsSanctionHit { get; set; }
    }

    public class KycResultDetailsRequest
    {
        [Required]
        public int QueryId { get; set; }
    }

    public class KycResultDetailsResponse
    {
        public List<string> PepExternalIds { get; set; }
        public List<string> SactionExternalIds { get; set; }
    }

    public class GetKycResultDetailsHistoryRequest
    {
        [Required]
        public int CustomerId { get; set; }
        [Required]
        public int HistoryDayCount { get; set; }
    }

    public class GetKycResultDetailsHistoryResponse
    {
        public List<GetKycResultDetailsHistoryResponseDate> QueryDatesWithListHits { get; set; }
    }

    public class GetKycResultDetailsHistoryResponseDate
    {
        public DateTime QueryDate { get; set; }
        public List<string> PepExternalIds { get; set; }
        public List<string> SanctionExternalIds { get; set; }
    }
}