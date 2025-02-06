using System;
using System.Collections.Concurrent;
using System.Threading;
namespace NTech.Services.Infrastructure
{
    public static class NTechPerServiceExclusiveLock
    {
        /// <summary>
        /// Used to prevent code i a specific module from running in parallell with itself.
        /// The same name cannot be used from two different services since the isolation is per service.
        /// </summary>
        public static T RunWithExclusiveLock<T>(string lockName, Func<T> ifLockAquired, Func<T> ifAlreadyLocked, TimeSpan? waitForLock = null)
        {
            var lockObject = locks.GetOrAdd(lockName, (_) => new object());
            bool lockTaken = false;
            if (waitForLock.HasValue)
                Monitor.TryEnter(lockObject, waitForLock.Value, ref lockTaken);
            else
                Monitor.TryEnter(lockObject, ref lockTaken);

            if (lockTaken)
            {
                try
                {
                    return ifLockAquired();
                }
                finally
                {
                    Monitor.Exit(lockObject);
                }
            }
            else
                return ifAlreadyLocked();
        }

        private static ConcurrentDictionary<string, object> locks = new ConcurrentDictionary<string, object>();
    }
}
