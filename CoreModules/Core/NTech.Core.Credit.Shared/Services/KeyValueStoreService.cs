using NTech.Core.Credit.Shared.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.Code.Services
{
    public class KeyValueStoreService : IKeyValueStoreService
    {
        private readonly Func<ICreditContextExtended> createContext;

        public KeyValueStoreService(Func<ICreditContextExtended> createContext)
        {
            this.createContext = createContext;
        }

        public void RemoveValue(string key, string keySpace, Action<bool> observeWasRemoved = null)
        {
            using (var context = createContext())
            {
                var c = context.KeyValueItemsQueryable.SingleOrDefault(x => x.Key == key && x.KeySpace == keySpace);
                var wasRemoved = RemoveValueComposable(context, key, keySpace); ;
                if (wasRemoved)
                {
                    context.SaveChanges();
                }

                observeWasRemoved?.Invoke(wasRemoved);
            }
        }

        public static bool RemoveValueComposable(ICreditContextExtended context, string key, string keySpace)
        {
            var c = context.KeyValueItemsQueryable.SingleOrDefault(x => x.Key == key && x.KeySpace == keySpace);
            var wasRemoved = c != null;
            if (wasRemoved)
            {
                context.RemoveKeyValueItem(c);
            }
            return wasRemoved;
        }

        public string GetValue(string key, string keySpace)
        {
            using (var context = createContext())
            {
                var c = context.KeyValueItemsQueryable.SingleOrDefault(x => x.Key == key && x.KeySpace == keySpace);
                return c?.Value;
            }
        }

        public void SetValue(string key, string keySpace, string value, Action<bool> observeWasUpdated = null)
        {
            using (var context = this.createContext())
            {
                var wasUpdated = SetValueComposable(context, key, keySpace, value);

                context.SaveChanges();

                observeWasUpdated?.Invoke(wasUpdated);
            }
        }

        public static bool SetValueComposable(ICreditContextExtended context, string key, string keySpace, string value)
        {
            var c = context.KeyValueItemsQueryable.SingleOrDefault(x => x.Key == key && x.KeySpace == keySpace);
            var wasUpdated = true;
            if (c == null)
            {
                wasUpdated = false;
                c = new KeyValueItem
                {
                    Key = key,
                    KeySpace = keySpace,
                };
                context.AddKeyValueItems(c);
            }
            context.FillInfrastructureFields(c);
            c.Value = value;
            return wasUpdated;
        }

        public Dictionary<string, string> GetValues(ISet<string> keys, string keySpace)
        {
            using (var context = createContext())
            {
                var baseQuery = context
                    .KeyValueItemsQueryable
                    .Where(x => x.KeySpace == keySpace);
                if (keys != null)
                    baseQuery = baseQuery.Where(x => keys.Contains(x.Key));

                var values = baseQuery
                    .Select(x => new { x.Key, x.Value })
                    .ToList()
                    .ToDictionary(x => x.Key, x => x.Value);

                return keys == null ? values : keys.ToDictionary(x => x, x => values.Opt(x));
            }
        }

        public Dictionary<string, string> GetAllValues(string keySpace) => GetValues(null, keySpace);
    }

    public interface IKeyValueStoreService
    {
        void RemoveValue(string key, string keySpace, Action<bool> observeWasRemoved = null);

        string GetValue(string key, string keySpace);

        Dictionary<string, string> GetValues(ISet<string> keys, string keySpace);
        Dictionary<string, string> GetAllValues(string keySpace);

        void SetValue(string key, string keySpace, string value, Action<bool> observeWasUpdated = null);
    }

    public enum KeyValueStoreKeySpaceCode
    {
        CreditManualPaymentsV1,
        MortgageLoanCollateralsV1,
        BookKeepingAccountNrsV1,
        SeMortgageLoanAmortzationBasisV1,
        LoanSettledMessageSentV1
    }
}