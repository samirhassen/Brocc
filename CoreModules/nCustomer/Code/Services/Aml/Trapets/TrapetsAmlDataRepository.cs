using nCustomer.DbModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer.Code.Services.Aml.Trapets
{
    public class TrapetsAmlDataRepository
    {
        public class TrapetsAmlItem
        {
            public DateTime CreationDate { get; set; }
            public string CivicRegNr { get; set; }
            public int CustomerId { get; set; }
            public DateTime ChangeDate { get; set; }
            public string AddressStreet { get; set; }
            public string AddressCity { get; set; }
            public string AddressZipcode { get; set; }
            public string AddressCountry { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Taxcountries { get; set; }
            public string ExternalPep { get; set; }
            public string Ispep { get; set; }
        }


        private class TmpItem
        {
            public int Id { get; set; }
            public int CustomerId { get; set; }
            public DateTimeOffset ChangeDate { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }
            public bool IsEncrypted { get; set; }
        }

        public Tuple<byte[], List<TrapetsAmlItem>> FetchTrapetsAmlData(byte[] latestSeenTimestamp, IList<int> customerIds)
        {
            var result = new List<TrapetsAmlItem>();
            byte[] newLatestSeenTimestamp;

            var encryptionKeys = NEnv.EncryptionKeys.AsDictionary();
            byte[] latestSeenTs = latestSeenTimestamp;
            var itemNamesToFetch = new string[] { "civicRegNr", "addressCity", "addressZipcode", "addressStreet", "addressCountry", "email", "taxcountries", "phone", "lastName", "firstName", "externalIsPep", "ispep" };

            using (var context = new CustomersContext())
            {
                var latestItemIdsPerBatch = new List<int>(); //So we can pick out the globally latest at the using the database ordering

                var baseQuery = context
                            .CustomerProperties
                            .AsNoTracking()
                            .Where(x => x.IsCurrentData && itemNamesToFetch.Contains(x.Name));

                IList<int> customerIdsToFetch;
                if (latestSeenTs != null)
                {
                    //This intermediate step is needed since we dont just want the changed items on the customers with changes but all the items but only for changed customers
                    var tmp = new List<int>();
                    foreach (var idsGroup in SplitIntoGroupsOfN((customerIds ?? new List<int>()).Distinct().ToArray(), 500))
                    {
                        tmp.AddRange(
                            baseQuery
                            .Where(x => idsGroup.Contains(x.CustomerId) && BinaryComparer.Compare(x.Timestamp, latestSeenTs) > 0)
                            .Select(x => x.CustomerId)
                            .ToList()
                            .Distinct());
                    }
                    customerIdsToFetch = tmp;
                }
                else
                {
                    customerIdsToFetch = (customerIds ?? new List<int>()).Distinct().ToList();
                }

                foreach (var idsGroup in SplitIntoGroupsOfN(customerIdsToFetch.ToArray(), 500))
                {
                    var q = baseQuery
                        .Where(x => idsGroup.Contains(x.CustomerId));

                    var creationDateByCustomerId = context
                        .CustomerProperties
                        .Where(x => idsGroup.Contains(x.CustomerId))
                        .GroupBy(x => x.CustomerId)
                        .Select(x => new
                        {
                            CustomerId = x.Key,
                            CreationDate = x.OrderBy(y => y.ChangedDate).Select(y => y.ChangedDate).FirstOrDefault()
                        })
                        .ToDictionary(x => x.CustomerId, x => x.CreationDate);

                    var idOfLatest = q.OrderByDescending(x => x.Timestamp).Select(x => (int?)x.Id).FirstOrDefault();
                    if (idOfLatest.HasValue) //There are any hits
                    {
                        latestItemIdsPerBatch.Add(idOfLatest.Value);
                        var items = q.Select(x => new TmpItem
                        {
                            Id = x.Id,
                            CustomerId = x.CustomerId,
                            ChangeDate = x.ChangedDate,
                            Name = x.Name,
                            Value = x.Value,
                            IsEncrypted = x.IsEncrypted
                        }).ToList();
                        var encryptedItemIds = items.Where(x => x.IsEncrypted).Select(x => long.Parse(x.Value)).ToArray();
                        var decryptedValues = EncryptionContext.Load(context, encryptedItemIds, encryptionKeys);
                        var resultItems = items
                            .GroupBy(x => x.CustomerId)
                            .Select(x => new
                            {
                                CustomerId = x.Key,
                                ChangeDate = x.OrderByDescending(y => y.ChangeDate).Select(y => y.ChangeDate).First(),
                                Items = x.GroupBy(y => y.Name).ToDictionary(y => y.Key, y => y.OrderByDescending(z => z.Id).First())
                            })
                            .Select(x =>
                            {
                                Func<string, string> getValue = name =>
                                {
                                    if (x.Items.ContainsKey(name))
                                    {
                                        var item = x.Items[name];
                                        return item.IsEncrypted
                                            ? decryptedValues[long.Parse(item.Value)]
                                            : item.Value;
                                    }
                                    else
                                        return null;
                                };
                                return new TrapetsAmlItem
                                {
                                    CreationDate = creationDateByCustomerId[x.CustomerId].Date,
                                    CustomerId = x.CustomerId,
                                    ChangeDate = x.ChangeDate.DateTime,
                                    CivicRegNr = getValue("civicRegNr"),
                                    AddressStreet = getValue("addressStreet"),
                                    AddressCity = getValue("addressCity"),
                                    AddressZipcode = getValue("addressZipcode"),
                                    AddressCountry = getValue("addressCountry"),
                                    Email = getValue("email"),
                                    Phone = getValue("phone"),
                                    FirstName = getValue("firstName"),
                                    LastName = getValue("lastName"),
                                    Taxcountries = getValue("taxcountries"),
                                    ExternalPep = getValue("externalIsPep"),
                                    Ispep = getValue("ispep")
                                };
                            });
                        result.AddRange(resultItems);
                    }
                }

                if (latestItemIdsPerBatch.Count > 0)
                {
                    newLatestSeenTimestamp = context.CustomerProperties.Where(x => latestItemIdsPerBatch.Contains(x.Id)).OrderByDescending(x => x.Timestamp).Select(x => x.Timestamp).First();
                }
                else
                    newLatestSeenTimestamp = null;
            }

            return Tuple.Create(newLatestSeenTimestamp, result);
        }

        private static class BinaryComparer
        {
            public static int Compare(byte[] b1, byte[] b2)
            {
                throw new NotImplementedException();
            }
        }

        private static IEnumerable<IEnumerable<T>> SplitIntoGroupsOfN<T>(T[] array, int n)
        {
            for (var i = 0; i < (float)array.Length / n; i++)
            {
                yield return array.Skip(i * n).Take(n);
            }
        }
    }
}