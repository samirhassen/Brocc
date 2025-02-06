using NTech;
using NTech.Core;
using NTech.Core.Module.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;

namespace nPreCredit
{
    public static class EncryptionContext
    {
        public static IDictionary<long, string> Load<TContext>(TContext context, long[] itemIds,
            IDictionary<string, string> encryptionKeysByName) where TContext : PreCreditContext
        {
            return EncryptionContext<TContext>.Load(context, itemIds, encryptionKeysByName);
        }

        public static void WithEncryptionExtended(Action<EncryptionContext<PreCreditContextExtended>> a, int currentUserId, IClock clock, string informationMetadata)
        {
            EncryptionContext<PreCreditContextExtended>.WithEncryption(a, () => new PreCreditContextExtended(currentUserId, clock, informationMetadata));
        }

        public static void WithEncryption(Action<EncryptionContext<PreCreditContext>> a)
        {
            EncryptionContext<PreCreditContext>.WithEncryption(a, () => new PreCreditContext());
        }

        public static TReturn WithEncryptionExtendedR<TReturn>(Func<EncryptionContext<PreCreditContextExtended>, TReturn> f, int currentUserId, IClock clock, string informationMetadata)
        {
            return EncryptionContext<PreCreditContextExtended>.WithEncryptionR(f, () => new PreCreditContextExtended(currentUserId, clock, informationMetadata));
        }
    }

    public class EncryptionContext<TContext> where TContext : PreCreditContext
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

        public static IDictionary<long, string> Load<TContextS>(TContextS context, long[] itemIds,
            IDictionary<string, string> encryptionKeysByName) where TContextS : PreCreditContext
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

        public static void WithEncryption<TContextS>(Action<EncryptionContext<TContextS>> a, Func<TContextS> createContext) where TContextS : PreCreditContext
        {
            WithEncryptionR<TContextS, object>(x =>
            {
                a(x);
                return (object)null;
            }, createContext);
        }

        public static TResult WithEncryptionR<TContextS, TResult>(Func<EncryptionContext<TContextS>, TResult> a, Func<TContextS> createContext) where TContextS : PreCreditContext
        {
            var e = new EncryptionContext<TContextS>();
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

        public static void SaveEncryptItemsShared<T>(
            T[] items,
            Func<T, string> getClearTextValue,
            Action<T, long> setEncryptedValueId,
            int createdById,
            string currentEncryptionKeyName,
            IDictionary<string, string> encryptionKeysByName,
            IClock clock,
            PreCreditContext context)
        {
            context.EnsureCurrentTransaction();

            EncryptionService.SaveEncryptItemsShared(items, getClearTextValue, setEncryptedValueId,
                createdById, currentEncryptionKeyName, encryptionKeysByName, CoreClock.SharedInstance, context);
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