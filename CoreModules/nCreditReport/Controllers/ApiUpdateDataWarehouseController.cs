using nCreditReport.Code;
using nCreditReport.DbModel;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCreditReport.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("Api")]
    public partial class ApiUpdateDataWarehouseController : NController
    {
        private static HashSet<string> ItemNameBlackList = new HashSet<string>(new List<string>()
            {   "birthDate", "civicRegNr", "firstName", "lastName", "addressStreet", "addressZipcode", "addressCity", "civicRegNrCountry",
                "orgnr", "companyName",
                "htmlReportArchiveKey", "xmlReportArchiveKey", "pdfReportArchiveKey"
            });

        [Route("DataWarehouse/Update")]
        [HttpPost]
        public ActionResult UpdateDataWarehouse()
        {
            var errors = new List<string>();
            int userId;
            string username;
            string metadata;
            GetUserProperties(errors, out userId, out username, out metadata);

            if (errors.Count > 0)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, string.Join(";", errors));

            Merge_Dimension_CreditReportItem(userId, metadata);

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        private void Merge_Dimension_CreditReportItem(int currentUserId, string informationmetadata)
        {
            Func<CreditReportContext, IQueryable<CreditReportItemDimensionModel>> query = c =>
                c
                .CreditApplicationHeaders
                .SelectMany(x => x.EncryptedItems.Select(y => new { header = x, item = y }))
                .Where(x => x.header.CustomerId.HasValue)
                .Select(x => new CreditReportItemDimensionModel
                {
                    Id = x.item.Id,
                    CreditReportHeaderId = x.header.Id,
                    CustomerId = x.header.CustomerId ?? 0,
                    CreditReportProviderName = x.header.CreditReportProviderName,
                    RequestDate = x.header.RequestDate,
                    Name = x.item.Name,
                    EncryptionKeyName = x.header.EncryptionKeyName,
                    Timestamp = x.item.Timestamp
                });

            var repo = new Lazy<CreditReportRepository>(() => new CreditReportRepository(NEnv.EncryptionKeys.CurrentKeyName, NEnv.EncryptionKeys.AsDictionary()));

            MergeDimension(
                    query,
                    SystemItemCode.DwLatestMergedTimestamp_Dimension_CreditReportItem2,
                    "CreditReportItem",
                    (items, c) =>
                        {
                            var decryptedItems = repo
                                .Value
                                .BulkFetchCreditReports(
                                    items.Select(x => Tuple.Create(x.EncryptionKeyName, x.Id)).ToList(),
                                    c);

                            var resultItems = items
                                .Where(x => !ItemNameBlackList.Contains(x.Name))
                                .Select(x => new
                                {
                                    Id = x.Id,
                                    CreditReportHeaderId = x.CreditReportHeaderId,
                                    CreditReportProviderName = x.CreditReportProviderName,
                                    RequestDate = x.RequestDate.DateTime,
                                    CustomerId = x.CustomerId,
                                    Name = x.Name,
                                    Value = decryptedItems[x.Id]
                                }).ToList();

                            return resultItems;
                        }, currentUserId, informationmetadata);
        }

        private class CreditReportItemDimensionModel : TimestampedItem
        {
            public int Id { get; set; }
            public int CreditReportHeaderId { get; set; }
            public string CreditReportProviderName { get; set; }
            public DateTimeOffset RequestDate { get; set; }
            public int CustomerId { get; set; }
            public string EncryptionKeyName { get; set; }
            public string Name { get; set; }
        }

        private void MergeDimension<T, U>(Func<CreditReportContext, IQueryable<T>> getBaseQuery, SystemItemCode code, string dimensionName, Func<List<T>, CreditReportContext, List<U>> toDwItems, int currentUserId, string informationMetadata) where T : TimestampedItem
        {
            Merge(getBaseQuery, code, dimensionName, toDwItems, false, currentUserId, informationMetadata);
        }

        private void MergeFact<T, U>(Func<CreditReportContext, IQueryable<T>> getBaseQuery, SystemItemCode code, string factName, Func<List<T>, CreditReportContext, List<U>> toDwItems, int currentUserId, string informationMetadata) where T : TimestampedItem
        {
            Merge(getBaseQuery, code, factName, toDwItems, true, currentUserId, informationMetadata);
        }

        private void Merge<T, U>(Func<CreditReportContext, IQueryable<T>> getBaseQuery, SystemItemCode code, string dimOrFactName, Func<List<T>, CreditReportContext, List<U>> toDwItems, bool isFact, int currentUserId, string informationMetadata) where T : TimestampedItem
        {
            var systemItemRepo = new SystemItemRepository(currentUserId, informationMetadata);

            byte[] latestSeenTs;
            byte[] maxTs;
            using (var context = new CreditReportContext())
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
                using (var context = new CreditReportContext())
                {
                    var q = getBaseQuery(context);

                    if (latestSeenTs != null)
                        q = q.Where(x => BinaryComparer.Compare(x.Timestamp, latestSeenTs) > 0);

                    var result = q.Where(x => BinaryComparer.Compare(x.Timestamp, maxTs) <= 0)
                        .OrderBy(x => x.Timestamp)
                        .Take(500);

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
                        systemItemRepo.SetTimestamp(code, newLatestSeenTs, context);
                    }

                    latestSeenTs = newLatestSeenTs;

                    context.SaveChanges();
                }
            }
            while (count > 0);
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
    }
}