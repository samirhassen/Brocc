using Dapper;
using Newtonsoft.Json;
using NTech.Core.Module.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace NTech.Core.Module.Shared.Services
{
    public class EncryptionService
    {
        private readonly string currentEncryptionKeyName;
        private readonly IDictionary<string, string> encryptionKeysByName;
        private readonly ICoreClock clock;
        private readonly INTechCurrentUserMetadata currentUser;

        public EncryptionService(string currentEncryptionKeyName,
            IDictionary<string, string> encryptionKeysByName,
            ICoreClock clock,
            INTechCurrentUserMetadata currentUser)
        {
            this.currentEncryptionKeyName = currentEncryptionKeyName;
            this.encryptionKeysByName = encryptionKeysByName;
            this.clock = clock;
            this.currentUser = currentUser;
        }

        public void SaveEncryptItems<T>(
                    T[] items,
                    Func<T, string> getClearTextValue,
                    Action<T, long> setEncryptedValueId,
                    INTechDbContext context) => SaveEncryptItemsShared(
                        items, getClearTextValue, setEncryptedValueId,
                        currentUser.UserId, currentEncryptionKeyName, encryptionKeysByName, clock, context);

        public static void SaveEncryptItemsShared<T>(
            T[] items,
            Func<T, string> getClearTextValue,
            Action<T, long> setEncryptedValueId,
            int createdById,
            string currentEncryptionKeyName,
            IDictionary<string, string> encryptionKeysByName,
            ICoreClock clock,
            INTechDbContext context)
        {
            context.EnsureCurrentTransaction();

            SaveEncryptItemsShared(items, getClearTextValue, setEncryptedValueId, createdById, currentEncryptionKeyName, encryptionKeysByName, clock, context.GetConnection(), context.CurrentTransaction);
        }

        private class IdAndKey { public long Id { get; set; } public string EncryptionKeyName { get; set; } }
        private class DecryptedItem { public long Id { get; set; } public string Value { get; set; } }

        public IDictionary<long, string> DecryptEncryptedValues(INTechDbContext context, long[] itemIds) =>
            DecryptEncryptedValuesShared(context.GetConnection(), itemIds, encryptionKeysByName,
                transaction: context.HasCurrentTransaction ? context.CurrentTransaction : null);

        private static IDictionary<long, string> DecryptEncryptedValuesShared(IDbConnection connection, long[] itemIds,
                    IDictionary<string, string> encryptionKeysByName, IDbTransaction transaction = null)
        {
            var result = new Dictionary<long, string>();
            if (itemIds.Length == 0)
                return result;

            var idsWithKeyNames = new List<IdAndKey>();
            long[] localItemIds = itemIds.DistinctPreservingOrder().ToArray();
            foreach (var idGroup in localItemIds.SplitIntoGroupsOfN(500))
            {
                idsWithKeyNames.AddRange(connection.Query<IdAndKey>("select e.Id, e.EncryptionKeyName from EncryptedValue e where e.Id in @idGroup", new { idGroup }, transaction: transaction));
            }
            var idsPerKey = idsWithKeyNames.GroupBy(x => x.EncryptionKeyName).ToDictionary(x => x.Key, x => x.Select(y => y.Id));
            foreach (var keyWithIds in idsPerKey)
            {
                var encryptionKeyName = keyWithIds.Key;
                var encryptionKeyValue = encryptionKeysByName[encryptionKeyName];
                foreach (var idSet in keyWithIds.Value.ToArray().SplitIntoGroupsOfN(500))
                {
                    var decryptedValues = connection.Query<DecryptedItem>(
                        "select a.Id, convert(nvarchar(max), DecryptByPassphrase(@passphrase, a.Value)) as Value from EncryptedValue a where a.Id in @idSet",
                        new
                        {
                            passphrase = encryptionKeyValue,
                            idSet = idSet
                        },
                        transaction: transaction);
                    foreach (var decryptedValue in decryptedValues)
                    {
                        result.Add(decryptedValue.Id, decryptedValue.Value);
                    }
                }
            }

            var missingIds = localItemIds.Where(x => !result.ContainsKey(x)).ToList();
            if (missingIds.Any())
                throw new Exception("These ids are missing: " + string.Join(", ", missingIds));
            else if (result.Count != localItemIds.Length)
                throw new Exception("Oversharing error");

            return result;
        }

        private static void SaveEncryptItemsShared<T>(
            T[] items,
            Func<T, string> getClearTextValue,
            Action<T, long> setEncryptedValueId,
            int createdById,
            string currentEncryptionKeyName,
            IDictionary<string, string> encryptionKeysByName,
            ICoreClock clock,
            System.Data.IDbConnection connection,
            IDbTransaction transaction)
        {
            foreach (var t in items)
            {
                var id = (long)connection.ExecuteScalar<decimal>(
                    "insert into EncryptedValue (Value, CreatedDate, CreatedById, EncryptionKeyName) values (EncryptByPassphrase(@PassPhrase, CONVERT(varbinary(max), @Value)), @CreatedDate, @CreatedById, @EncryptionKeyName);SELECT SCOPE_IDENTITY()",
                    new
                    {
                        Value = getClearTextValue(t),
                        EncryptionKeyName = currentEncryptionKeyName,
                        PassPhrase = encryptionKeysByName[currentEncryptionKeyName],
                        CreatedDate = clock.Now,
                        CreatedById = createdById
                    }, transaction: transaction);

                setEncryptedValueId(t, id);
            }
        }
    }

    public class EncryptionKeySet
    {
        public static EncryptionKeySet ParseFromString(string text)
        {
            return JsonConvert.DeserializeObject<EncryptionKeySet>(text);
        }

        public string CurrentKeyName { get; set; }

        public List<KeyItem> AllKeys { get; set; }

        public string GetKey(string name)
        {
            return AllKeys.Single(x => x.Name == name).Key;
        }

        public Dictionary<string, string> AsDictionary()
        {
            return AllKeys.ToDictionary(x => x.Name, x => x.Key);
        }

        public class KeyItem
        {
            public string Name { get; set; }
            public string Key { get; set; }
        }
    }
}
