using System;
using System.Collections.Concurrent;
using System.Threading;

namespace NTech.Services.Infrastructure;

public static class NTechPerServiceExclusiveLock
{
    private static readonly ConcurrentDictionary<string, object> Locks = new();

    /// <summary>
    /// Used to prevent code i a specific module from running in parallell with itself.
    /// The same name cannot be used from two different services since the isolation is per service.
    /// </summary>
    public static T RunWithExclusiveLock<T>(string lockName, Func<T> ifLockAcquired, Func<T> ifAlreadyLocked,
        TimeSpan? acquireTimeout = null)
    {
        var lockObject = Locks.GetOrAdd(lockName, _ => new object());
        bool lockTaken = false;
        if (acquireTimeout.HasValue)
            Monitor.TryEnter(lockObject, acquireTimeout.Value, ref lockTaken);
        else
            Monitor.TryEnter(lockObject, ref lockTaken);

        if (!lockTaken) return ifAlreadyLocked();

        try
        {
            return ifLockAcquired();
        }
        finally
        {
            Monitor.Exit(lockObject);
        }
    }
}