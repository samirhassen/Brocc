using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace NTech.Services.Infrastructure
{
    /// <summary>
    /// Queues items to be handled in batches.
    /// - Batches are processed either when a certain time has passed regadless of current size or when a certain size is reached
    /// - Respects hosting shutdown requests
    /// </summary>
    /// <summary>
    /// Queues items to be handled in batches.
    /// - Batches are processed either when a certain time has passed regadless of current size or when a certain size is reached
    /// - Respects hosting shutdown requests
    /// </summary>
    public class BatchingHostingAwareQueue<T> : IDisposable
    {
        private int sizeLimit;
        private ConcurrentQueue<T> queue;
        private object initLock = new object();
        private bool isInitDone = false;
        private readonly TimeSpan queueTickRate;
        private readonly TimeSpan timeLimit;
        private readonly Action<IList<T>, CancellationToken> processBatch;
        private readonly ITaskScheduler taskScheduler;
        public bool HasExited { get; set; }

        public BatchingHostingAwareQueue(Action<IList<T>, CancellationToken> processBatch, int sizeLimit, TimeSpan timeLimit, TimeSpan? queueTickRate = null, ITaskScheduler taskScheduler = null)
        {
            this.sizeLimit = sizeLimit;
            this.timeLimit = timeLimit;
            this.processBatch = processBatch;
            this.queueTickRate = queueTickRate ?? TimeSpan.FromSeconds(5);
            this.queue = new ConcurrentQueue<T>();
            this.taskScheduler = taskScheduler ?? new AspNetHostingEnvironmentTaskScheduler();
        }

        public void Dispose()
        {

        }

        private void Init()
        {
            if(!isInitDone)
            {
                lock(initLock)
                {
                    if (!isInitDone)
                    {
                        taskScheduler.QueueBackgroundWorkItem(ct =>
                        {
                            try
                            {
                                DateTimeOffset nextForcedSendTime = DateTimeOffset.UtcNow.Add(timeLimit);
                                while (true)
                                {
                                    WaitHandle.WaitAny(new[] { ct.WaitHandle }, (int)queueTickRate.TotalMilliseconds);

                                    if (DateTimeOffset.UtcNow >= nextForcedSendTime || queue.Count >= sizeLimit || ct.IsCancellationRequested)
                                    {
                                        nextForcedSendTime = DateTimeOffset.UtcNow.Add(timeLimit);
                                        var items = new List<T>();
                                        T item;
                                        while (queue.TryDequeue(out item))
                                        {
                                            items.Add(item);
                                        }
                                        if (items.Count > 0)
                                        {
                                            processBatch(items, ct);
                                        }
                                    }

                                    if (ct.IsCancellationRequested)
                                        break;
                                }
                            }
                            finally
                            {
                                HasExited = true;
                            }                            
                        });
                        isInitDone = true;
                    }
                }
            }
        }

        public void Enqueue(T item)
        {            
            queue.Enqueue(item);
            Init();
        }

        public void Enqueue(IList<T> items)
        {            
            foreach (var i in items)
            {
                queue.Enqueue(i);
            }
            Init();
        }
    }

    public interface ITaskScheduler
    {
        void QueueBackgroundWorkItem(Action<CancellationToken> workItem);
    }

    public class AspNetHostingEnvironmentTaskScheduler : ITaskScheduler
    {
        public void QueueBackgroundWorkItem(Action<CancellationToken> workItem)
        {
            HostingEnvironment.QueueBackgroundWorkItem(workItem);
        }
    }
}
