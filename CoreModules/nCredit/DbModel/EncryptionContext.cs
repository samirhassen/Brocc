using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;

namespace nCredit
{
    public class EncryptionContext
    {
        public CreditContext Context
        {
            get
            {
                return encryptionContext.Context;
            }
        }

        private EncryptionContext<CreditContext> encryptionContext;

        private EncryptionContext(EncryptionContext<CreditContext> encryptionContext)
        {
            this.encryptionContext = encryptionContext;
        }

        public static IDictionary<long, string> Load(CreditContext context, long[] itemIds,
            IDictionary<string, string> encryptionKeysByName)
        {
            return EncryptionContext<CreditContext>.Load(context, itemIds, encryptionKeysByName);
        }

        public static void WithEncryption(Action<EncryptionContext> a)
        {
            EncryptionContext<CreditContext>.WithEncryption(x =>
            {
                var c = new EncryptionContext(x);
                a(c);
            }, () => new CreditContext());
        }

        public static T WithEncryption<T>(Func<EncryptionContext, T> a)
        {
            return EncryptionContext<CreditContext>.WithEncryption(x =>
            {
                var c = new EncryptionContext(x);
                return a(c);
            }, () => new CreditContext());
        }

        public static void WithEncryptionExtended(Action<EncryptionContext<CreditContextExtended>> a, Func<CreditContextExtended> createContext)
        {
            EncryptionContext<CreditContextExtended>.WithEncryption(a, createContext);
        }

        public static T WithEncryptionExtended<T>(Func<EncryptionContext<CreditContextExtended>, T> a, Func<CreditContextExtended> createContext)
        {
            return EncryptionContext<CreditContextExtended>.WithEncryption(a, createContext);
        }

        public void SaveEncryptItems<T>(
            T[] items,
            Func<T, string> getClearTextValue,
            Action<T, long> setEncryptedValueId,
            int createdById,
            string currentEncryptionKeyName,
            IDictionary<string, string> encryptionKeysByName)
        {
            encryptionContext.SaveEncryptItems(items, getClearTextValue, setEncryptedValueId, createdById, currentEncryptionKeyName, encryptionKeysByName);
        }

        public static void SharedSaveEncryptItems<T>(
            T[] items,
            Func<T, string> getClearTextValue,
            Action<T, long> setEncryptedValueId,
            int createdById,
            CreditContext context)
        {
            EncryptionContext<CreditContext>.SharedSaveEncryptItems(items, getClearTextValue, setEncryptedValueId, createdById, context);
        }
    }

    public class EncryptionContext<TContext> where TContext : CreditContext
    {
        public TContext Context { get; set; }

        private EncryptionContext()
        {

        }

        private class DecryptedItem
        {
            public long Id { get; set; }
            public string Value { get; set; }
        }

        public static IDictionary<long, string> Load(TContext context, long[] itemIds,
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

        public static void WithEncryption(Action<EncryptionContext<TContext>> a, Func<TContext> createContext)
        {
            var e = new EncryptionContext<TContext>();
            using (e.Context = createContext())
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

        public static T WithEncryption<T>(Func<EncryptionContext<TContext>, T> a, Func<TContext> createContext)
        {
            var e = new EncryptionContext<TContext>();
            using (e.Context = createContext())
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
                var p = new SqlParameter()
                {
                    ParameterName = "@id",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.Output
                };
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
            CreditContext context)
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
                    new SqlParameter("@CreatedDate", DateTimeOffset.Now),
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