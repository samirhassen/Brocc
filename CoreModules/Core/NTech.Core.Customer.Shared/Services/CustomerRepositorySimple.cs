using NTech.Core.Customer.Shared.Database;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer
{
    public class CustomerRepositorySimple
    {
        private readonly ICustomerContext db;
        private readonly EncryptionService encryptionService;

        public CustomerRepositorySimple(
            ICustomerContext db,
            EncryptionService encryptionService)
        {
            this.db = db;
            this.encryptionService = encryptionService;
        }

        private static IEnumerable<IEnumerable<T>> SplitIntoGroupsOfN<T>(T[] array, int n)
        {
            for (var i = 0; i < (float)array.Length / n; i++)
            {
                yield return array.Skip(i * n).Take(n);
            }
        }

        public Dictionary<int, Dictionary<string, string>> BulkFetchD(ISet<int> customerIds, ISet<string> propertyNames = null, bool skipDecryptingEncryptedItems = false)
        {
            var r = BulkFetch(customerIds, propertyNames: propertyNames, skipDecryptingEncryptedItems: skipDecryptingEncryptedItems);
            return r?.ToDictionary(
                x => x.Key,
                x => x.Value.ToDictionary(y => y.Name, y => y.Value));
        }

        public IDictionary<int, IList<CustomerPropertyModel>> BulkFetch(ISet<int> customerIds, ISet<string> propertyNames = null, bool skipDecryptingEncryptedItems = false)
        {
            var result = new Dictionary<int, IList<CustomerPropertyModel>>();

            foreach (var g in SplitIntoGroupsOfN(customerIds.ToArray(), 200))
            {
                var tmpProps = db
                    .CustomerPropertiesQueryable
                    .Where(y => g.Contains(y.CustomerId) && y.IsCurrentData);

                if (propertyNames != null)
                {
                    var ns = propertyNames == null ? null : propertyNames.ToArray();
                    tmpProps = tmpProps.Where(y => ns.Contains(y.Name));
                }

                var props = tmpProps
                    .Select(y => new
                    {
                        y.IsEncrypted,
                        Property = new CustomerPropertyModel
                        {
                            CustomerId = y.CustomerId,
                            Name = y.Name,
                            Group = y.Group,
                            IsSensitive = y.IsSensitive,
                            Value = y.Value
                        }
                    })
                    .ToList();
                if (!skipDecryptingEncryptedItems)
                {
                    var needsDecryption = props.Where(x => x.IsEncrypted).ToList();

                    var decryptedValues = encryptionService.DecryptEncryptedValues(
                        db,
                        needsDecryption.Select(x => long.Parse(x.Property.Value)).ToArray());

                    foreach (var v in needsDecryption)
                    {
                        v.Property.Value = decryptedValues[long.Parse(v.Property.Value)];
                    }
                }

                foreach (var customerIdGroup in props.GroupBy(x => x.Property.CustomerId))
                {
                    result[customerIdGroup.Key] = customerIdGroup.Select(x => x.Property).ToList();
                }
            }

            return result;
        }

        public ISet<T> AsSet<T>(params T[] args)
        {
            return args?.ToHashSetShared();
        }

        public IList<CustomerPropertyModel> GetProperties(int customerId, List<string> onlyTheseNames = null, bool skipDecryptingEncryptedItems = false)
        {
            //TODO: This seems strange. Get rid of all uses of this
            HashSet<string> names = null;
            if (onlyTheseNames != null)
                names = new HashSet<string>(onlyTheseNames);
            var result = BulkFetch(new HashSet<int>() { customerId }, propertyNames: names, skipDecryptingEncryptedItems: skipDecryptingEncryptedItems);

            if (result.ContainsKey(customerId))
                return result[customerId];
            else
                return new List<CustomerPropertyModel>();
        }

        public IList<CustomerPropertyModel> GetDecryptedProperties(int customerId, List<string> onlyTheseNames)
        {
            HashSet<string> names = null;
            if (onlyTheseNames != null)
                names = new HashSet<string>(onlyTheseNames);
            var result = BulkFetch(new HashSet<int>() { customerId }, propertyNames: names);

            if (result.ContainsKey(customerId))
                return result[customerId];
            else
                return new List<CustomerPropertyModel>();
        }

        public Tuple<CustomerPropertyModelExtended, List<CustomerPropertyModelExtended>> GetCurrentAndHistoricalValuesForProperty(int customerId, string name, Func<string, string> getUserDisplayNameByUserId)
        {
            var items = db
                .CustomerPropertiesQueryable
                .Where(x => x.CustomerId == customerId && x.Name == name)
                .Select(x => new
                {
                    x.IsCurrentData,
                    x.IsEncrypted,
                    Item = new CustomerPropertyModelExtended
                    {
                        ChangeDate = x.ChangedDate,
                        ChangedById = x.ChangedById,
                        CustomerId = x.CustomerId,
                        Group = x.Group,
                        Id = x.Id,
                        IsSensitive = x.IsSensitive,
                        Name = x.Name,
                        Value = x.Value,
                        CreatedByBusinessEventId = x.CreatedByBusinessEventId
                    }
                })
                .ToList()
                .OrderByDescending(x => x.IsCurrentData ? 1 : 0)
                .ThenByDescending(x => x.Item.Id)
                .ToList();

            var encryptedItems = items.Where(x => x.IsEncrypted).ToList();
            IDictionary<long, string> decryptedValues = null;
            if (encryptedItems.Any())
            {
                decryptedValues = encryptionService.DecryptEncryptedValues(db,
                    encryptedItems.Select(x => long.Parse(x.Item.Value)).ToArray());

                foreach (var i in encryptedItems)
                {
                    i.Item.Value = decryptedValues[long.Parse(i.Item.Value)];
                }
            }
            if (getUserDisplayNameByUserId != null)
            {
                foreach (var i in items)
                    i.Item.ChangedByDisplayName = getUserDisplayNameByUserId(i.Item.ChangedById.ToString());
            }

            var currentItem = items.FirstOrDefault();
            var historicalItems = items.Select(x => x.Item).ToList(); //Includes the current item because it becomes super confusing when it doesnt

            return Tuple.Create(currentItem?.Item, historicalItems);
        }

        public CustomerPropertyModel GetInsensitiveProperty(int customerId, string name)
        {
            var result = BulkFetch(new HashSet<int>() { customerId }, propertyNames: new HashSet<string>() { name });

            if (!result.ContainsKey(customerId))
                return null;

            var prop = result[customerId]
                .Where(x => !x.IsSensitive)
                .SingleOrDefault();

            return prop;
        }

        public CustomerPropertyModel GetSensitiveProperty(int customerId, string name)
        {
            var result = BulkFetch(new HashSet<int>() { customerId }, propertyNames: new HashSet<string>() { name });

            if (!result.ContainsKey(customerId))
                return null;

            var prop = result[customerId]
                .Where(x => x.IsSensitive)
                .SingleOrDefault();

            return prop;
        }

        public List<int> FindCustomersByExactFirstName(string name)
        {
            return db.CustomerPropertiesQueryable
                .Where(x => x.IsCurrentData && x.Name == "firstName" && (x.Value == name || x.Value.Contains(" " + name + " ") || x.Value.StartsWith(name + " ") || x.Value.EndsWith(" " + name)))
                .Select(x => x.CustomerId)
                .ToList();
        }

        public List<int> FindCustomersByExactCompanyName(string name)
        {
            return db.CustomerPropertiesQueryable
                .Where(x => x.Name == "companyName" && x.Value == name)
                .Select(x => x.CustomerId)
                .ToList();
        }
    }
}