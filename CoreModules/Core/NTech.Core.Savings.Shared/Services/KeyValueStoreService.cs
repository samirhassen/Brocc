using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Savings.Shared.Database;
using NTech.Core.Savings.Shared.DbModel;

namespace NTech.Core.Savings.Shared.Services
{
    public class KeyValueStoreService : IKeyValueStoreService
    {
        private readonly Func<ISavingsContext> createContext;
        private readonly SavingsContextFactory contextFactory;
        private readonly ICoreClock clock;
        private readonly INTechCurrentUserMetadata user;

        public KeyValueStoreService(SavingsContextFactory contextFactory, ICoreClock clock, INTechCurrentUserMetadata user)
        {
            createContext = () => contextFactory.CreateContext();
            this.contextFactory = contextFactory;
            this.clock = clock;
            this.user = user;
        }

        public void RemoveValue(string key, string keySpace, Action<bool> observeWasRemoved = null)
        {
            using (var context = createContext())
            {
                var c = context.KeyValueItemsQueryable.SingleOrDefault(x => x.Key == key && x.KeySpace == keySpace);
                var wasRemoved = false;
                if (c != null)
                {
                    context.RemoveKeyValueItems(c);
                    context.SaveChanges();
                }

                observeWasRemoved?.Invoke(wasRemoved);
            }
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
                var wasUpdated = SetValueComposable(context, key, keySpace, value, user.UserId, user.InformationMetadata, clock);

                context.SaveChanges();

                observeWasUpdated?.Invoke(wasUpdated);
            }
        }

        public static bool SetValueComposable(ISavingsContext context, string key, string keySpace, string value, int userId, string informationmetadata, ICoreClock clock)
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
            c.ChangedById = userId;
            c.ChangedDate = clock.Now;
            c.InformationMetaData = informationmetadata;
            c.Value = value;
            return wasUpdated;
        }

        public Dictionary<string, string> GetValues(ISet<string> keys, string keySpace)
        {
            using (var context = createContext())
            {
                return GetValuesComposable(context, keys, keySpace);
            }
        }

        public static Dictionary<string, string> GetValuesComposable(ISavingsContext context, ISet<string> keys, string keySpace)
        {
            var values = context
                    .KeyValueItemsQueryable
                    .Where(x => keys.Contains(x.Key) && x.KeySpace == keySpace)
                    .Select(x => new { x.Key, x.Value })
                    .ToList()
                    .ToDictionary(x => x.Key, x => x.Value);

            return keys.ToDictionary(x => x, x => values.Opt(x));
        }

        public T SetConcurrent<T>(string key, string keySpace, Func<T> createNew, Func<T, T> mergeOnExists) where T : class
        {
            var attempt = 0;
            while (true)
            {
                attempt++;
                try
                {
                    return SetConcurrentI(key, keySpace, createNew, mergeOnExists);
                }
                catch (Exception ex)
                {
                    if (contextFactory.IsConcurrencyException(ex))
                    {
                        if (attempt > 5)
                            throw;
                    }
                    else
                        throw;
                }
            }
        }

        private T SetConcurrentI<T>(string key, string keySpace, Func<T> createNew, Func<T, T> mergeOnExists) where T : class
        {
            T newValue;
            using (var context = createContext())
            {
                var existing = context.KeyValueItemsQueryable.SingleOrDefault(x => x.Key == key && x.KeySpace == keySpace);
                if (existing == null)
                {
                    newValue = createNew();
                    context.AddKeyValueItems(new KeyValueItem
                    {
                        Key = key,
                        KeySpace = keySpace,
                        Value = JsonConvert.SerializeObject(createNew()),
                        ChangedById = user.UserId,
                        ChangedDate = clock.Now,
                        InformationMetaData = user.InformationMetadata
                    });
                }
                else
                {
                    var oldValue = JsonConvert.DeserializeObject<T>(existing.Value);
                    newValue = mergeOnExists(oldValue);
                    existing.Value = JsonConvert.SerializeObject(newValue);
                }
                context.SaveChanges();
                return newValue;
            }
        }
    }
}