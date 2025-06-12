using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Services.Infrastructure
{
    public static class NTechCache
    {
        private static readonly Lazy<MemoryCache> cache = new Lazy<MemoryCache>(() => MemoryCache.Default);

        public static T WithCache<T>(string key, TimeSpan duration, Func<T> produce) where T : class
        {
            var val = Get<T>(key);
            if (val != null)
                return val;
            val = produce();
            Set(key, val, duration);
            return val;
        }

        private class StructWrapper<T>
        {
            public T Item { get; set; }
        }

        public static T WithCacheS<T>(string key, TimeSpan duration, Func<T> produce) where T : struct
        {
            var val = Get<StructWrapper<T>>(key);
            if (val != null)
                return val.Item;
            val = new StructWrapper<T> { Item = produce() };
            Set(key, val, duration);
            return val.Item;
        }

        private const string SignallingName = "ntech.cache.v1";

        public static void Set<T>(string key, T value, TimeSpan duration) where T : class
        {
            var cip = new CacheItemPolicy();
            cip.AbsoluteExpiration = DateTimeOffset.Now.Add(duration);
            cip.ChangeMonitors.Add(new SignaledChangeMonitor(SignallingName));
            cache.Value.Set(key, value, cip);
        }

        public static void ClearCache()
        {
            SignaledChangeMonitor.Signal(SignallingName);
        }

        public static bool Remove(string key)
        {
            if (!cache.IsValueCreated)
                return false;
            return cache.Value.Remove(key) != null;
        }

        public static T Get<T>(string key) where T : class
        {
            return cache.Value.Get(key) as T;
        }

        internal class SignaledChangeEventArgs : EventArgs
        {
            public string Name { get; private set; }
            public SignaledChangeEventArgs(string name = null) { this.Name = name; }
        }

        /// <summary>
        /// Cache change monitor that allows an app to fire a change notification
        /// to all associated cache items.
        /// </summary>
        internal class SignaledChangeMonitor : ChangeMonitor
        {
            // Shared across all SignaledChangeMonitors in the AppDomain
            private static event EventHandler<SignaledChangeEventArgs> Signaled;

            private string _name;
            private string _uniqueId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            public override string UniqueId
            {
                get { return _uniqueId; }
            }

            public SignaledChangeMonitor(string name = null)
            {
                _name = name;
                // Register instance with the shared event
                SignaledChangeMonitor.Signaled += OnSignalRaised;
                base.InitializationComplete();
            }

            public static void Signal(string name = null)
            {
                if (Signaled != null)
                {
                    // Raise shared event to notify all subscribers
                    Signaled(null, new SignaledChangeEventArgs(name));
                }
            }

            protected override void Dispose(bool disposing)
            {
                SignaledChangeMonitor.Signaled -= OnSignalRaised;
            }

            private void OnSignalRaised(object sender, SignaledChangeEventArgs e)
            {
                if (string.IsNullOrWhiteSpace(e.Name) || string.Compare(e.Name, _name, true) == 0)
                {
                    // Cache objects are obligated to remove entry upon change notification.
                    base.OnChanged(null);
                }
            }
        }
    }
}
