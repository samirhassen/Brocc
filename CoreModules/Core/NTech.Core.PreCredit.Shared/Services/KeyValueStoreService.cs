using Newtonsoft.Json;
using nPreCredit.DbModel;
using NTech.Core.PreCredit.Shared;
using System;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class KeyValueStoreService : IKeyValueStoreService
    {
        private readonly IPreCreditContextFactoryService preCreditContextFactoryService;

        public KeyValueStoreService(IPreCreditContextFactoryService preCreditContextFactoryService)
        {
            this.preCreditContextFactoryService = preCreditContextFactoryService;
        }

        private IPreCreditContextExtended createContext()
        {
            return preCreditContextFactoryService.CreateExtended();
        }

        public T UpdateOnlyConcurrent<T>(string key, string keySpace, Func<T, T> merge) where T : class
        {
            return SetConcurrent(key, keySpace, () =>
                {
                    throw new Exception($"Entity '{key}' must already exist");
                }, merge);
        }

        public T InsertOnlyConcurrent<T>(string key, string keySpace, T item) where T : class
        {
            return SetConcurrent(key, keySpace, () => item, _ =>
            {
                throw new Exception($"Entity '{key}' already exists");
            });
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
                    //Used to catch System.Data.Entity.Infrastructure.DbUpdateConcurrencyException but we cant share that between legacy and core so this should be workable instead.
                    if (ex.GetType().Name.Contains("Concurrency"))
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
            using (var context = preCreditContextFactoryService.CreateExtended())
            {
                var c = context.KeyValueItemsQueryable.SingleOrDefault(x => x.Key == key && x.KeySpace == keySpace);
                if (c == null)
                {
                    newValue = createNew();
                    context.AddKeyValueItems(context.FillInfrastructureFields(new KeyValueItem
                    {
                        Key = key,
                        KeySpace = keySpace,
                        Value = JsonConvert.SerializeObject(createNew())
                    }));
                }
                else
                {
                    var oldValue = JsonConvert.DeserializeObject<T>(c.Value);
                    newValue = mergeOnExists(oldValue);
                    c.Value = JsonConvert.SerializeObject(newValue);
                }
                context.SaveChanges();
                return newValue;
            }
        }

        public void RemoveValue(string key, string keySpace, Action<bool> observeWasRemoved = null)
        {
            using (var context = createContext())
            {
                var wasRemoved = RemoveValueComposable(context, key, keySpace);
                if (wasRemoved)
                    context.SaveChanges();

                observeWasRemoved?.Invoke(wasRemoved);
            }
        }

        public static bool RemoveValueComposable(IPreCreditContextExtended context, string key, string keySpace)
        {
            var c = context.KeyValueItemsQueryable.SingleOrDefault(x => x.Key == key && x.KeySpace == keySpace);
            var wasRemoved = false;
            if (c != null)
            {
                context.RemoveKeyValueItems(c);
                wasRemoved = true;
            }

            return wasRemoved;
        }

        string IKeyValueStoreService.GetValue(string key, string keySpace)
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

        public static string GetValueComposable(IPreCreditContextExtended context, string key, string keySpace)
        {
            return context.KeyValueItemsQueryable.SingleOrDefault(x => x.Key == key && x.KeySpace == keySpace)?.Value;
        }

        public static bool SetValueComposable(IPreCreditContextExtended context, string key, string keySpace, string value)
        {
            var c = context.KeyValueItemsQueryable.SingleOrDefault(x => x.Key == key && x.KeySpace == keySpace);
            var wasUpdated = true;
            if (c == null)
            {
                wasUpdated = false;
                c = new KeyValueItem
                {
                    Key = key,
                    KeySpace = keySpace
                };
                context.FillInfrastructureFields(c);
                context.AddKeyValueItems(c);
            }
            c.Value = value;
            return wasUpdated;
        }
    }

    public interface IKeyValueStoreService
    {
        void RemoveValue(string key, string keySpace, Action<bool> observeWasRemoved = null);

        string GetValue(string key, string keySpace);

        void SetValue(string key, string keySpace, string value, Action<bool> observeWasUpdated = null);

        T SetConcurrent<T>(string key, string keySpace, Func<T> createNew, Func<T, T> mergeOnExists) where T : class;

        T UpdateOnlyConcurrent<T>(string key, string keySpace, Func<T, T> merge) where T : class;

        T InsertOnlyConcurrent<T>(string key, string keySpace, T item) where T : class;
    }

    public class KeyValueStore
    {
        private readonly string keySpace;
        private readonly IKeyValueStoreService keyValueStoreService;

        public KeyValueStore(string keySpace, IKeyValueStoreService keyValueStoreService)
        {
            this.keySpace = keySpace;
            this.keyValueStoreService = keyValueStoreService;
        }

        public KeyValueStore(KeyValueStoreKeySpaceCode keySpaceCode, IKeyValueStoreService keyValueStoreService) : this(keySpaceCode.ToString(), keyValueStoreService)
        {
        }

        public void RemoveValue(string key, Action<bool> observeWasRemoved = null)
        {
            keyValueStoreService.RemoveValue(key, keySpace, observeWasRemoved: observeWasRemoved);
        }

        public string GetValue(string key)
        {
            return keyValueStoreService.GetValue(key, keySpace);
        }

        public void SetValue(string key, string value, Action<bool> observeWasUpdated = null)
        {
            keyValueStoreService.SetValue(key, keySpace, value, observeWasUpdated: observeWasUpdated);
        }
    }

    public class DocumentDatabase<T> where T : class
    {
        private readonly string keySpace;
        private readonly IKeyValueStoreService keyValueStoreService;

        public DocumentDatabase(string keySpace, IKeyValueStoreService keyValueStoreService)
        {
            this.keySpace = keySpace;
            this.keyValueStoreService = keyValueStoreService;
        }

        public DocumentDatabase(KeyValueStoreKeySpaceCode keySpaceCode, IKeyValueStoreService keyValueStoreService) : this(keySpaceCode.ToString(), keyValueStoreService)
        {
        }

        public void Remove(string key, Action<bool> observeWasRemoved = null)
        {
            this.keyValueStoreService.RemoveValue(key, keySpace, observeWasRemoved: observeWasRemoved);
        }

        public T Get(string key)
        {
            var v = keyValueStoreService.GetValue(key, keySpace);
            return v == null ? null : JsonConvert.DeserializeObject<T>(v);
        }

        public T SetConcurrent(string key, Func<T> createNew, Func<T, T> mergeOnExists)
        {
            return keyValueStoreService.SetConcurrent(key, keySpace, createNew, mergeOnExists);
        }

        public T UpdateOnlyConcurrent(string key, Func<T, T> merge)
        {
            return keyValueStoreService.UpdateOnlyConcurrent(key, keySpace, merge);
        }

        public T InsertOnlyConcurrent(string key, T item)
        {
            return keyValueStoreService.InsertOnlyConcurrent(key, keySpace, item);
        }

        public bool SetComposable(IPreCreditContextExtended context, string key, T item)
        {
            return KeyValueStoreService.SetValueComposable(context, key, keySpace, JsonConvert.SerializeObject(item));
        }

        public bool RemoveComposable(IPreCreditContextExtended context, string key)
        {
            return KeyValueStoreService.RemoveValueComposable(context, key, keySpace);
        }
    }
}