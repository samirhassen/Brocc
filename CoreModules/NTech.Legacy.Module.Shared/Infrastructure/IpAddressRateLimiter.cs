using System;
using System.Collections.Concurrent;

namespace NTech.Legacy.Module.Shared.Infrastructure
{
    /// <summary>
    /// Keeps track of the number of failed authentication attemps per ip within a five minute window
    /// and blocks all attempts for five minutes if this count reaches five. The counter resets if a login succeeds.
    /// </summary>
    public class IpAddressRateLimiter
    {
        private readonly Func<DateTimeOffset> utcNow;
        private ConcurrentDictionary<string, Tuple<int, DateTimeOffset>> authenticationAttemptHistory = new ConcurrentDictionary<string, Tuple<int, DateTimeOffset>>();
        const int BlockCount = 5;

        public IpAddressRateLimiter() : this(() => DateTimeOffset.UtcNow)
        {

        }

        public IpAddressRateLimiter(Func<DateTimeOffset> utcNow)
        {
            this.utcNow = utcNow;
        }

        public void LogAuthenticationAttempt(string callerIpAddress, bool wasSuccessful)
        {
            if (!wasSuccessful)
            {
                authenticationAttemptHistory.AddOrUpdate(callerIpAddress,
                    (_) => Tuple.Create(1, utcNow()),
                    (_, history) => Tuple.Create(history.Item1 < BlockCount ? history.Item1 + 1 : BlockCount, utcNow()));
            }
            else
            {
                authenticationAttemptHistory.TryRemove(callerIpAddress, out _);
            }
        }

        public bool IsIpRateLimited(string callerIpAddress)
        {
            if (string.IsNullOrWhiteSpace(callerIpAddress))
                return false;
            Cleanup();
            return authenticationAttemptHistory.TryGetValue(callerIpAddress, out var history) && history.Item1 >= BlockCount;
        }

        private void Cleanup()
        {
            foreach (var ipAddress in authenticationAttemptHistory.Keys)
            {
                if (authenticationAttemptHistory.TryGetValue(ipAddress, out var history))
                {
                    //Reset history after 5 minutes if no failed attempts come in
                    if (history.Item2 < utcNow().AddMinutes(-5))
                        authenticationAttemptHistory.TryRemove(ipAddress, out _);
                }
            }
        }
    }
}
