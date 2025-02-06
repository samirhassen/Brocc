using System;

namespace nTest.RandomDataSource
{
    public class RandomnessSource : IRandomnessSource
    {
        private readonly Random random;
        private object randomLock = new object();

        public RandomnessSource(int? seed)
        {
            if (seed.HasValue)
                random = new Random(seed.Value);
            else
                random = new Random();
        }

        public double NextDouble()
        {
            lock (randomLock)
            {
                return random.NextDouble();
            }
        }

        public int NextIntBetween(int from, int to)
        {
            lock (randomLock)
            {
                return random.Next(from, to + 1);
            }
        }

        public decimal NextDecimal(decimal from, decimal to)
        {
            lock (randomLock)
            {
                var next = random.NextDouble();

                var s = (double)(to - from);
                return (decimal)(NextDouble() * s + ((double)from));
            }
        }

        public bool ShouldHappenWithProbability(decimal p)
        {
            if (p < 0m || p > 1m)
                throw new Exception("Probability must be between 0 and 1");
            lock (randomLock)
            {
                return ((decimal)random.NextDouble()) <= p;
            }
        }

        public T OneOf<T>(params T[] args)
        {
            return args[NextIntBetween(0, args.Length - 1)];
        }
    }
}