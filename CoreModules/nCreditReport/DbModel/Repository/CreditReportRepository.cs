using nCreditReport.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace nCreditReport
{
    public class CreditReportRepository
    {
        protected readonly string currentEncryptionKeyName;
        protected readonly IDictionary<string, string> encryptionKeysByName;

        public CreditReportRepository(
            string currentEncryptionKeyName,
            IDictionary<string, string> encryptionKeysByName)
        {
            this.currentEncryptionKeyName = currentEncryptionKeyName;
            this.encryptionKeysByName = encryptionKeysByName;
        }

        public class FindResult
        {
            public class Item
            {
                public int CreditReportId { get; set; }
                public DateTimeOffset RequestDate { get; set; }
                public string ProviderName { get; set; }
            }

            public List<Item> Items { get; set; }
        }
        private static IEnumerable<IEnumerable<T>> SplitIntoGroupsOfN<T>(T[] array, int n)
        {
            for (var i = 0; i < (float)array.Length / n; i++)
            {
                yield return array.Skip(i * n).Take(n);
            }
        }

        public FindResult FindForProvider(int customerId, string providerName)
        {
            using (var context = new CreditReportContext())
            {
                var items = context.CreditApplicationHeaders.Where(x => x.CustomerId == customerId && x.CreditReportProviderName == providerName).Select(x => new FindResult.Item
                {
                    CreditReportId = x.Id,
                    RequestDate = x.RequestDate
                }).ToList();

                return new FindResult
                {
                    Items = items
                };
            }
        }

        public FindResult FindForProviders(string[] providers, int customerId)
        {
            using (var context = new CreditReportContext())
            {
                var items = context.CreditApplicationHeaders.Where(x => x.CustomerId == customerId && providers.Contains(x.CreditReportProviderName))
                    .Select(x => new FindResult.Item
                    {
                        CreditReportId = x.Id,
                        RequestDate = x.RequestDate,
                        ProviderName = x.CreditReportProviderName
                    }).ToList();

                return new FindResult
                {
                    Items = items
                };
            }
        }

        public class FetchResult
        {
            public int CreditReportId { get; set; }
            public DateTimeOffset RequestDate { get; set; }
            public class Item
            {
                public string Name { get; set; }
                public string Value { get; set; }
            }
            public List<Item> Items { get; set; }
            public int? CustomerId { get; set; }
            public string ProviderName { get; set; }
        }

        public FetchResult FetchAll(int creditReportId)
        {
            List<string> names;
            using (var context = new CreditReportContext())
            {
                names = context
                    .CreditApplicationHeaders
                    .Where(x => x.Id == creditReportId)
                    .SelectMany(x => x.EncryptedItems.Select(y => y.Name))
                    .ToList();
            }
            return Fetch(creditReportId, names);
        }

        public FetchResult Fetch(int creditReportId, List<string> requestedItems)
        {
            var localRequestedItems = requestedItems;

            using (var context = new CreditReportContext())
            {
                var header = context
                    .CreditApplicationHeaders
                    .SingleOrDefault(x => x.Id == creditReportId);
                if (header == null)
                {
                    return null;
                }

                var items = new List<FetchResult.Item>();
                var encryptionKey = encryptionKeysByName[header.EncryptionKeyName];

                List<SqlParameter> parameters = new List<SqlParameter>();
                parameters.Add(new SqlParameter("@passphrase", encryptionKey));
                parameters.Add(new SqlParameter("@creditReportId", header.Id));

                parameters.AddRange(localRequestedItems.Select((x, i) => new SqlParameter("@n" + i, x)));
                var pList = string.Join(",", localRequestedItems.Select((_, i) => "@n" + i));

                items.AddRange(context.Database.SqlQuery<FetchResult.Item>(
                    string.Format(@"select  a.Name,
                                            convert(nvarchar(max), DecryptByPassphrase(@passphrase, a.Value)) as Value
                        from   EncryptedCreditReportItem a
                        where  a.Name in ({0})
                        and    a.CreditReportHeaderId = @creditReportId", pList),
                    parameters.ToArray()));

                return new FetchResult
                {
                    CustomerId = header.CustomerId,
                    ProviderName = header.CreditReportProviderName,
                    CreditReportId = creditReportId,
                    RequestDate = header.RequestDate,
                    Items = items
                };
            }
        }

        public Dictionary<int, string> BulkFetchCreditReports(List<Tuple<string, int>> creditReportKeyNamesAndItemIds, CreditReportContext context)
        {
            var groups = creditReportKeyNamesAndItemIds
                .GroupBy(x => x.Item1)
                .Select(x => new
                {
                    EncryptionKeyName = x.Key,
                    ItemsIds = x.Select(y => y.Item2).ToArray()
                })
                .ToList();

            var result = new Dictionary<int, string>();
            foreach (var group in groups)
            {
                foreach (var itemIds in SplitIntoGroupsOfN(group.ItemsIds, 300))
                {
                    var encryptionKey = encryptionKeysByName[group.EncryptionKeyName];

                    List<SqlParameter> parameters = new List<SqlParameter>();
                    parameters.Add(new SqlParameter("@passphrase", encryptionKey));

                    parameters.AddRange(group.ItemsIds.Select((x, i) => new SqlParameter("@n" + i, x)));
                    var pList = string.Join(",", group.ItemsIds.Select((_, i) => "@n" + i));

                    var items = context.Database.SqlQuery<BulkFetchItem>(
                        string.Format(@"select  a.Id,
                                        convert(nvarchar(max), DecryptByPassphrase(@passphrase, a.Value)) as Value
                    from   EncryptedCreditReportItem a
                    where  a.Id in ({0})", pList),
                        parameters.ToArray());

                    foreach (var item in items)
                        result[item.Id] = item.Value;
                }
            }

            return result;
        }

        private class BulkFetchItem
        {
            public int Id { get; set; }
            public string Value { get; set; }
        }



        public int Save(SaveCreditReportRequest request, int customerId)
        {
            if (customerId <= 0)
                throw new Exception("Missing CustomerId");

            using (var context = new CreditReportContext())
            {
                var tx = context.Database.BeginTransaction();
                try
                {
                    Action<InfrastructureBaseItem> setInfraProperties = ii =>
                        {
                            ii.ChangedDate = request.CreationDate;
                            ii.ChangedById = request.ChangedById;
                            ii.InformationMetaData = ii.InformationMetaData;
                        };
                    var header = new CreditReportHeader
                    {
                        CreditReportProviderName = request.CreditReportProviderName,
                        EncryptionKeyName = currentEncryptionKeyName,
                        RequestDate = request.RequestDate,
                        CustomerId = customerId,
                        InformationMetaData = request.InformationMetaData
                    };
                    setInfraProperties(header);

                    var searchTerms = request.SearchTerms.Select(x =>
                        {
                            var i = new CreditReportSearchTerm
                            {
                                CreditReport = header,
                                Name = x.Name,
                                Value = x.Value,
                                InformationMetaData = request.InformationMetaData
                            };
                            setInfraProperties(i);
                            return i;
                        });

                    context.CreditApplicationHeaders.Add(header);
                    context.CreditApplicationSearchTerms.AddRange(searchTerms);

                    context.SaveChanges();

                    if (!encryptionKeysByName.ContainsKey(header.EncryptionKeyName))
                        throw new Exception($"Missing enceyption key named {header.EncryptionKeyName}");
                    var encKey = encryptionKeysByName[header.EncryptionKeyName];
                    foreach (var item in request.Items)
                    {
                        context.Database.ExecuteSqlCommand(
                            "insert into EncryptedCreditReportItem (CreditReportHeaderId, Name, Value, ChangedDate, ChangedById, InformationMetaData) values (@CreditReportId, @Name, EncryptByPassphrase(@PassPhrase, CONVERT(varbinary(max), @Value)), @ChangedDate, @ChangedById, @InformationMetaData)",
                            new SqlParameter("@CreditReportId", header.Id),
                            new SqlParameter("@Name", item.Name),
                            new SqlParameter("@PassPhrase", encKey),
                            new SqlParameter("@Value", item.Value),
                            new SqlParameter("@ChangedDate", request.CreationDate),
                            new SqlParameter("@ChangedById", request.ChangedById),
                            new SqlParameter("@InformationMetaData", request.InformationMetaData)
                            );
                    }

                    tx.Commit();

                    return header.Id;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }
    }
}