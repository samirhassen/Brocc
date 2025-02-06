using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NTech.Services.Infrastructure
{
    public class RingBuffer<T> : IEnumerable<T>
    {
        private readonly ConcurrentQueue<T> q;
        private readonly int maxSize;
        private object writeLock = new object();

        public RingBuffer(int maxSize)
        {
            if (maxSize < 1)
                throw new ArgumentException("must be >= 1", "maxSize");
            this.q = new ConcurrentQueue<T>();
            this.maxSize = maxSize;
        }

        public void Add(T item)
        {
            lock (writeLock)
            {
                T _;
                if (q.Count >= maxSize)
                    q.TryDequeue(out _);
                q.Enqueue(item);
            }
        }

        public void Clear()
        {
            lock (writeLock)
            {
                var guard = 0;
                T _;
                while (q.TryDequeue(out _) && guard++ < maxSize * 2)
                {

                }
                if (guard > maxSize + 1)
                    throw new Exception("Hit guard code!");
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return q.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return q.GetEnumerator();
        }
    }
}
