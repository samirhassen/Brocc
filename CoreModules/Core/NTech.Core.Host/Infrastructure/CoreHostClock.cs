using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;

namespace NTech.Core.Host.Infrastructure
{
    public class CoreHostClock : ICoreClock
    {
        private readonly Lazy<ICoreClock> productionInstance = new Lazy<ICoreClock>(() => new ProductionClock());
        private readonly Lazy<TestClock> testInstance;

        private static bool wasInintialized = false;
        private NEnv env;

        public CoreHostClock(NEnv env, ITestClient testClient)
        {
            this.env = env;
            testInstance = new Lazy<TestClock>(() => new TestClock(testClient));
        }

        public static void EnsureInitialized()
        {
            if (!wasInintialized)
                throw new Exception("Attempting to use NClock during startup!");
        }

        public static void RegisterAfterStartupInit(IHostApplicationLifetime hostApplicationLifetime)
        {
            hostApplicationLifetime.ApplicationStarted.Register(() =>
            {
                //Used to have the startup code blow up if it calls into the clock during startup ... this is to ensure
                //that services dont cause infinite call chains during startup as nTest must be started for all the other clocks to work
                //would be better if we could break this so that some tiny thing that could be more "always" up hosted the clock for all.
                wasInintialized = true;
            });
        }

        public void ResetTestClock()
        {
            if (env.IsProduction)
                return;
            if (!testInstance.IsValueCreated)
                return;
            testInstance.Value.Reset();
        }

        public DateTimeOffset Now => ActualClock.Now;
        public DateTime Today => ActualClock.Today;

        private ICoreClock ActualClock
        {
            get
            {
                EnsureInitialized();
                return env.IsProduction ? productionInstance.Value : testInstance.Value;
            }
        }

        public void OnTimeMachineUpdate(DateTimeOffset d)
        {
            if (env.IsProduction)
                return;

            if (!wasInintialized)
                return; //This is ok since test test instance will pick up the initial time from the test module so it will get the correct time still.

            testInstance.Value.Set(d);

            return;
        }

        private class ProductionClock : ICoreClock
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

        private class TestClock : ICoreClock
        {
            private DateTimeOffset? now;
            private readonly ITestClient testClient;

            public TestClock(ITestClient testClient)
            {
                this.testClient = testClient;
            }

            public DateTimeOffset Now
            {
                get
                {
                    if (now.HasValue)
                        return now.Value;

                    var newNow = testClient.GetCurrentTime();
                    if (!newNow.HasValue)
                        throw new Exception("Failed to get current time from the test module");

                    Set(newNow.Value);

                    return now.Value;
                }
            }

            public DateTime Today => Now.ToLocalTime().Date;

            public void Set(DateTimeOffset now) => this.now = now;

            public void Reset() => this.now = null;
        }
    }
}
