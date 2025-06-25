using System.Collections.Concurrent;
using NTech.Services.Infrastructure;

namespace NTech.Core.Host.Logging
{
    public abstract class
        HostingAwareBackgroundBatchedServiceRequestQueueService<TItem> : HostingAwareBackgroundTimerService
    {
        protected override int TimeBetweenTicksInMilliseconds => 15000;
        private readonly ConcurrentQueue<TItem> queue = new();
        protected virtual int MaxBatchSize => 500;

        protected override async Task OnTick(CancellationToken cancellationToken)
        {
            var countLeft = queue.Count;
            while (countLeft > 0)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                var batch = new List<TItem>(MaxBatchSize);
                while (batch.Count < MaxBatchSize && queue.TryDequeue(out var i))
                {
                    batch.Add(i);
                }

                if (batch.Count <= 0) continue;
                countLeft -= batch.Count;
                if (cancellationToken.IsCancellationRequested)
                    return;
                await HandleBatch(batch, cancellationToken);
            }
        }

        protected abstract Task HandleBatch(List<TItem> items, CancellationToken cancellationToken);

        public void AddItem(TItem item)
        {
            queue.Enqueue(item);
        }

        public void AddItems(List<TItem> items)
        {
            items.ForEach(queue.Enqueue);
        }
    }
}