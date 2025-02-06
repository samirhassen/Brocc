using NTech;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;

namespace nCustomer.DbModel
{
    public class EncryptionContext
    {
        private EncryptionContext()
        {

        }

        private class DecryptedItem
        {
            public long Id { get; set; }
            public string Value { get; set; }
        }

        public static IDictionary<long, string> Load(CustomersContext context, long[] itemIds,
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
                    List<SqlParameter> parameters = new List<SqlParameter>();
                    parameters.Add(new SqlParameter("@passphrase", encryptionKeyValue));

                    parameters.AddRange(idSet.Select((x, i) => new SqlParameter("@pId" + i, x)));
                    var pList = string.Join(",", idSet.Select((_, i) => "@pId" + i));

                    foreach (var i in context.Database.SqlQuery<DecryptedItem>(
                        string.Format(@"select a.Id, 
                                 convert(nvarchar(max), DecryptByPassphrase(@passphrase, a.Value)) as Value
                          from   EncryptedValue a
                          where    a.Id in ({0})", pList), parameters.ToArray()))
                    {
                        result.Add(i.Id, i.Value);
                    }
                }
            }

            var missingIds = itemIds.Where(x => !result.ContainsKey(x)).ToList();
            if (missingIds.Any())
                throw new Exception("These ids are missing: " + string.Join(", ", missingIds));
            else if (result.Count != itemIds.Length)
                throw new Exception("Oversharing error");
            return result;
        }

        public static void SharedSaveEncryptItems<T>(
            T[] items,
            Func<T, string> getClearTextValue,
            Action<T, long> setEncryptedValueId,
            int createdById,
            CustomersContext context,
            IClock clock)
        {
            if (context.Database.CurrentTransaction == null)
            {
                throw new Exception("This methods writes directly to the database so it needs bo done in an ambient transaction.");
            }
            var e = NEnv.EncryptionKeys;
            var currentKeyName = e.CurrentKeyName;
            var keys = e.AsDictionary();
            foreach (var t in items)
            {
                var p = new SqlParameter()
                {
                    ParameterName = "@id",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.Output
                };
                var id = (long)context.Database.SqlQuery<decimal>(
                    "insert into EncryptedValue (Value, CreatedDate, CreatedById, EncryptionKeyName) values (EncryptByPassphrase(@PassPhrase, CONVERT(varbinary(max), @Value)), @CreatedDate, @CreatedById, @EncryptionKeyName);SELECT SCOPE_IDENTITY()",
                    new SqlParameter("@Value", getClearTextValue(t)),
                    new SqlParameter("@EncryptionKeyName", currentKeyName),
                    new SqlParameter("@PassPhrase", keys[currentKeyName]),
                    new SqlParameter("@CreatedDate", clock.Now),
                    new SqlParameter("@CreatedById", createdById)).Single();

                setEncryptedValueId(t, id);
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