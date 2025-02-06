using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTech.Core.Module.Shared.Infrastructure;
using System;

namespace TestsnPreCredit
{
    [TestClass]
    public class FewItemsCacheTests
    {
        private const string Key = "x";
        private const string ExpectedInitialValue = "a";
        private const string ExpectedFinalValue = "b";
        private static TimeSpan OneSecond = TimeSpan.FromSeconds(1);
        private static TimeSpan CacheDuration = TimeSpan.FromSeconds(5);


        private string ProduceInitial()
        {
            return ExpectedInitialValue;
        }

        private string ProduceFinal()
        {
            return ExpectedFinalValue;
        }

        [TestMethod]
        public void EmptyCache_ReturnsInitialValue()
        {
            var testClock = new TestClock();
            var cache = new FewItemsCache(() => testClock.UtcNow);

            var value = cache.WithCache(Key, CacheDuration, ProduceInitial);

            Assert.AreEqual(ExpectedInitialValue, value);
        }

        [TestMethod]
        public void InitialCache_WithNonExpired_ReturnsInitialValue()
        {
            var testClock = new TestClock();
            var cache = new FewItemsCache(() => testClock.UtcNow);
            cache.WithCache(Key, CacheDuration, ProduceInitial);
            testClock.MoveForward(CacheDuration - OneSecond);

            var value = cache.WithCache(Key, CacheDuration, ProduceFinal);

            Assert.AreEqual(ExpectedInitialValue, value);
        }

        [TestMethod]
        public void InitialCache_Cleared_ReturnsFinalValue()
        {
            var testClock = new TestClock();
            var cache = new FewItemsCache(() => testClock.UtcNow);
            cache.WithCache(Key, CacheDuration, ProduceInitial);
            cache.ClearCache();

            var value = cache.WithCache(Key, CacheDuration, ProduceFinal);

            Assert.AreEqual(ExpectedFinalValue, value);
        }

        [TestMethod]
        public void InitialCache_WithExpired_ReturnsFinalValue()
        {
            var testClock = new TestClock();
            var cache = new FewItemsCache(() => testClock.UtcNow);
            cache.WithCache(Key, CacheDuration, ProduceInitial);
            testClock.MoveForward(CacheDuration + OneSecond);

            var value = cache.WithCache(Key, CacheDuration, ProduceFinal);

            Assert.AreEqual(ExpectedFinalValue, value);
        }

        protected class TestClock
        {
            private DateTimeOffset now = new DateTimeOffset(2022, 12, 10, 1, 1, 1, 1, TimeSpan.FromHours(2));

            public DateTimeOffset UtcNow
            {
                get
                {
                    return now;
                }
            }

            public void MoveForward(TimeSpan timeSpan)
            {
                now = now.Add(timeSpan);
            }
        }
    }
}
