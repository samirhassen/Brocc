using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using static nCredit.Excel.DocumentClientExcelRequest;

namespace NTech.Core.Module.Shared.Infrastructure
{
    /// <summary>
    /// BEWARE: Dont use this for cases where there is potential for a really large nr of items.
    /// This is intended to replace NTechCache when sharing with dotnetcore for things like usernames.
    /// Not hooking into the aspnet-cache makes it easier to share but the prize is that the size is
    /// no longer managed by the infratructure.
    /// </summary>
    public class FewItemsCache
    {
        private readonly Func<DateTimeOffset> getUtcNow;
        private ConcurrentDictionary<string, CacheItem> cache;

        public FewItemsCache() : this(() => DateTimeOffset.UtcNow)
        {

        }

        //NOTE: This is for unit testing, not the time machine.
        public FewItemsCache(Func<DateTimeOffset> getUtcNow)
        {
            this.getUtcNow = getUtcNow;
            cache = new ConcurrentDictionary<string, CacheItem>();
        }

        public async Task<T> WithCacheAsync<T>(string key, Func<Task<(T Value, DateTimeOffset ExpirationDate)>> produceAsync)
        {
            if (cache.TryGetValue(key, out var item) && !item.IsExpired(getUtcNow()))
                return (T)item.Value;

            var newValueAndExpirationDate = await produceAsync();

            cache[key] = new CacheItem
            {
                ExpirationDate = newValueAndExpirationDate.ExpirationDate.ToUniversalTime(),
                Value = newValueAndExpirationDate.Value
            };

            return newValueAndExpirationDate.Value;
        }

        public Task<T> WithCacheAsync<T>(string key, TimeSpan duration, Func<Task<T>> produceAsync) => WithCacheAsync(key, async () => (
            Value: await produceAsync(),
            ExpirationDate: getUtcNow().Add(duration)));

        public T WithCache<T>(string key, TimeSpan duration, Func<T> produce)
        {
            CacheItem ProduceNew()
            {
                var value = produce();
                return new CacheItem
                {
                    ExpirationDate = getUtcNow().Add(duration),
                    Value = value
                };
            }
            var item = cache.AddOrUpdate(key,
                _ => ProduceNew(),
                (_, currentItem) =>
                {
                    if (currentItem.IsExpired(getUtcNow()))
                    {
                        return ProduceNew();
                    }
                    else
                    {
                        return currentItem;
                    }
                });
            return (T)item.Value;
        }

        private static Lazy<FewItemsCache> sharedInstance = new Lazy<FewItemsCache>(() => new FewItemsCache());
        public static FewItemsCache SharedInstance
        {
            get
            {
                return sharedInstance.Value;
            }
        }

        public void ClearCache()
        {
            cache.Clear();
        }

        private class CacheItem
        {
            public DateTimeOffset ExpirationDate { get; set; }
            public object Value { get; set; }

            public bool IsExpired(DateTimeOffset now)
            {
                return now > ExpirationDate;
            }
        }
    }
}
