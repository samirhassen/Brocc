using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace NTech.Legacy.Module.Shared;

#nullable enable

public static class NTechSemaphoreManager
{
    private const int DefaultConcurrency = 1;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    /// <summary>
    /// Used to prevent code in specific module from running in parallell with itself.
    /// The same name cannot be used from two different services since the isolation is per service.
    /// </summary>
    public static T RunWithExclusiveLock<T>(string lockName,
        Func<T> onLockAcquired,
        Func<T> onAlreadyLocked,
        TimeSpan? acquireTimeout = null,
        CancellationToken ct = default)
    {
        var lockAcquired = TryAcquire(lockName, acquireTimeout, ct, out var handle);

        if (!lockAcquired) return onAlreadyLocked();

        using (handle)
        {
            return onLockAcquired();
        }
    }

    public static async Task<T> RunWithExclusiveLockAsync<T>(string lockName,
        Func<CancellationToken, Task<T>> onLockAcquired,
        Func<CancellationToken, Task<T>> onAlreadyLocked,
        TimeSpan? acquireTimeout = null,
        CancellationToken ct = default)
    {
        var (lockAcquired, handle) = await TryAcquireAsync(lockName, acquireTimeout, ct);

        if (!lockAcquired) return await onAlreadyLocked(ct);

        using (handle)
        {
            return await onLockAcquired(ct);
        }
    }

    private static bool TryAcquire(string lockName, TimeSpan? timeout, CancellationToken ct,
        out SemaphoreHandle? releaser)
    {
        var semaphore = Locks.GetOrAdd(lockName, _ => new SemaphoreSlim(DefaultConcurrency));
        var to = timeout ?? TimeSpan.Zero;

        if (semaphore.Wait(to, ct))
        {
            releaser = new SemaphoreHandle(semaphore);
            return true;
        }

        releaser = null;
        return false;
    }

    private static async Task<(bool, SemaphoreHandle?)> TryAcquireAsync(string lockName, TimeSpan? timeout,
        CancellationToken ct)
    {
        var semaphore = Locks.GetOrAdd(lockName, _ => new SemaphoreSlim(DefaultConcurrency));
        var to = timeout ?? TimeSpan.Zero;

        if (await semaphore.WaitAsync(to, ct))
        {
            return (true, new SemaphoreHandle(semaphore));
        }

        return (false, null);
    }
}

internal class SemaphoreHandle(SemaphoreSlim semaphore) : IDisposable
{
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        semaphore.Release();
        _disposed = true;
    }
}