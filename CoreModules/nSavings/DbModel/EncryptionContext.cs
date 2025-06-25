using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using nSavings.Code;

namespace nSavings.DbModel
{
    public class EncryptionContext
    {
        public SavingsContext Context { get; set; }

        private EncryptionContext()
        {
        }

        private class DecryptedItem
        {
            public long Id { get; set; }
            public string Value { get; set; }
        }

        public static IDictionary<long, string> Load(SavingsContext context, long[] itemIds,
            IDictionary<string, string> encryptionKeysByName)
        {
            var result = new Dictionary<long, string>();

            var idsWithKeyNames = new List<Tuple<long, string>>();
            foreach (var idGroup in SplitIntoGroupsOfN(itemIds, 500))
            {
                idsWithKeyNames.AddRange(
                    context
                        .EncryptedValues
                        .Where(x => idGroup.Contains(x.Id))
                        .Select(x => new { x.Id, x.EncryptionKeyName })
                        .ToList()
                        .Select(x => Tuple.Create(x.Id, x.EncryptionKeyName)));
            }

            var idsPerKey = idsWithKeyNames.GroupBy(x => x.Item2).ToDictionary(x => x.Key, x => x.Select(y => y.Item1));
            foreach (var keyWithIds in idsPerKey)
            {
                var encryptionKeyName = keyWithIds.Key;
                var encryptionKeyValue = encryptionKeysByName[encryptionKeyName];
                foreach (var idSet in SplitIntoGroupsOfN(keyWithIds.Value.ToArray(), 500))
                {
                    var parameters = new List<SqlParameter> { new SqlParameter("@passphrase", encryptionKeyValue) };

                    parameters.AddRange(idSet.Select((x, i) => new SqlParameter("@pId" + i, x)));
                    var pList = string.Join(",", idSet.Select((_, i) => "@pId" + i));

                    foreach (var i in context.Database.SqlQuery<DecryptedItem>(
                                 $@"select a.Id, 
                                 convert(nvarchar(max), DecryptByPassphrase(@passphrase, a.Value)) as Value
                          from   EncryptedValue a
                          where    a.Id in ({pList})", parameters.ToArray()))
                    {
                        result.Add(i.Id, i.Value);
                    }
                }
            }

            var missingIds = itemIds.Where(x => !result.ContainsKey(x)).ToList();
            if (missingIds.Any())
                throw new Exception("These ids are missing: " + string.Join(", ", missingIds));
            if (result.Count != itemIds.Length)
                throw new Exception("Oversharing error");
            return result;
        }

        public static void WithEncryption(Action<EncryptionContext> a)
        {
            var e = new EncryptionContext();
            using (e.Context = new SavingsContext())
            {
                var tx = e.Context.Database.BeginTransaction();
                try
                {
                    a(e);
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public static T WithEncryption<T>(Func<EncryptionContext, T> a)
        {
            var e = new EncryptionContext();
            using (e.Context = new SavingsContext())
            {
                var tx = e.Context.Database.BeginTransaction();
                try
                {
                    var result = a(e);
                    tx.Commit();
                    return result;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public void SaveEncryptItems<T>(
            T[] items,
            Func<T, string> getClearTextValue,
            Action<T, long> setEncryptedValueId,
            int createdById,
            string currentEncryptionKeyName,
            IDictionary<string, string> encryptionKeysByName)
        {
            foreach (var t in items)
            {
                var id = (long)Context.Database.SqlQuery<decimal>(
                    "insert into EncryptedValue (Value, CreatedDate, CreatedById, EncryptionKeyName) values (EncryptByPassphrase(@PassPhrase, CONVERT(varbinary(max), @Value)), @CreatedDate, @CreatedById, @EncryptionKeyName);SELECT SCOPE_IDENTITY()",
                    new SqlParameter("@Value", getClearTextValue(t)),
                    new SqlParameter("@EncryptionKeyName", currentEncryptionKeyName),
                    new SqlParameter("@PassPhrase", encryptionKeysByName[currentEncryptionKeyName]),
                    new SqlParameter("@CreatedDate", DateTimeOffset.Now),
                    new SqlParameter("@CreatedById", createdById)).Single();

                setEncryptedValueId(t, id);
            }
        }

        public static void SharedSaveEncryptItems<T>(
            T[] items,
            Func<T, string> getClearTextValue,
            Action<T, long> setEncryptedValueId,
            int createdById,
            SavingsContext context)
        {
            if (context.Database.CurrentTransaction == null)
            {
                throw new Exception(
                    "This methods writes directly to the database so it needs bo done in an ambient transaction.");
            }

            var e = NEnv.EncryptionKeys;
            var currentKeyName = e.CurrentKeyName;
            var keys = e.AsDictionary();
            foreach (var t in items)
            {
                var id = (long)context.Database.SqlQuery<decimal>(
                    "insert into EncryptedValue (Value, CreatedDate, CreatedById, EncryptionKeyName) values (EncryptByPassphrase(@PassPhrase, CONVERT(varbinary(max), @Value)), @CreatedDate, @CreatedById, @EncryptionKeyName);SELECT SCOPE_IDENTITY()",
                    new SqlParameter("@Value", getClearTextValue(t)),
                    new SqlParameter("@EncryptionKeyName", currentKeyName),
                    new SqlParameter("@PassPhrase", keys[currentKeyName]),
                    new SqlParameter("@CreatedDate", DateTimeOffset.Now),
                    new SqlParameter("@CreatedById", createdById)).Single();

                setEncryptedValueId(t, id);
            }
        }

        private static IEnumerable<List<T>> SplitIntoGroupsOfN<T>(T[] array, int n)
        {
            for (var i = 0; i < (float)array.Length / n; i++)
            {
                yield return array.Skip(i * n).Take(n).ToList();
            }
        }
    }
}