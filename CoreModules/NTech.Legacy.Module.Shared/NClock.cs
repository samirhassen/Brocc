using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using System;

namespace NTech
{
    public interface IClock
    {
        DateTimeOffset Now { get; }
        DateTime Today { get; }
    }

    public static class ClockFactory
    {
        private static Lazy<IClock> productionInstance = new Lazy<IClock>(() => new ProductionClock());
        private static Lazy<TestClock> testInstance = new Lazy<TestClock>(() => new TestClock());

        private static bool wasInintialized = false;

        /// <summary>
        /// Call this last in the setup of all services
        /// </summary>
        public static void Init()
        {
            //Used to have the startup code blow up if it calls into the clock during startup ... this is to ensure
            //that services dont cause infinite call chains during startup as nTest must be started for all the other clocks to work
            //would be better if we could break this so that some tiny thing that could be more "always" up hosted the clock for all.
            wasInintialized = true;
        }

        internal static void EnsureInitialized()
        {
            if (!ClockFactory.wasInintialized)
                throw new Exception("Attempting to use NClock during startup!");
        }

        public static void ResetTestClock()
        {
            if (NTechEnvironment.Instance.IsProduction)
                return;
            if (!testInstance.IsValueCreated)
                return;
            testInstance.Value.Reset();
        }

        public static IClock SharedInstance
        {
            get
            {
                return NTechEnvironment.Instance.IsProduction ? productionInstance.Value : testInstance.Value;
            }
        }

        private class ProductionClock : IClock
        {
            public DateTimeOffset Now
            {
                get
                {
                    return DateTimeOffset.Now;
                }
            }

            public DateTime Today
            {
                get
                {
                    return DateTime.Today;
                }
            }
        }

        private class TestClock : IClock
        {
            private DateTimeOffset? now;
            private static Lazy<ServiceClient> testClient = new Lazy<ServiceClient>(() =>
                LegacyServiceClientFactory.CreateClientFactory(NTechEnvironment.Instance.ServiceRegistry).CreateClient(AnonymousHttpServiceUser.SharedInstance, "nTest"));

            public DateTimeOffset Now
            {
                get
                {
                    if (now.HasValue)
                        return now.Value;

                    ClockFactory.EnsureInitialized();

                    var newNow = testClient.Value.ToSync(() => testClient.Value.Call(
                        x => x.PostJson("Api/TimeMachine/GetCurrentTime", new { }),
                        x => x.ParseJsonAsAnonymousType(new { currentTime = (DateTimeOffset?)null })))?.currentTime;

                    if (!newNow.HasValue)
                        throw new Exception("Failed to get current time from the test module");

                    Set(newNow.Value);

                    return now.Value;
                }
            }

            public DateTime Today
            {
                get
                {
                    return Now.ToLocalTime().Date;
                }
            }

            public void Set(DateTimeOffset now)
            {
                this.now = now;
            }

            public void Reset()
            {
                this.now = null;
            }
        }

        public static bool TrySetApplicationDateAndTime(DateTimeOffset d)
        {
            if (NTechEnvironment.Instance.IsProduction)
                return false;

            testInstance.Value.Set(d);

            return true;
        }
    }
}