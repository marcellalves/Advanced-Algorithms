﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Advanced.Algorithms.Distributed
{
    /// <summary>
    ///     A simple asynchronous multi-thread supporting producer/consumer FIFO queue with minimal locking.
    /// </summary>
    internal class AsyncQueue<T>
    {
        //data queue.
        private readonly Queue<T> queue = new Queue<T>();

        //consumer task queue & lock.
        private readonly Queue<TaskCompletionSource<T>> consumerQueue = new Queue<TaskCompletionSource<T>>();
        private SemaphoreSlim consumerQueueLock = new SemaphoreSlim(1);

        private CancellationToken taskCancellationToken;

        internal AsyncQueue(CancellationToken taskCancellationToken = default(CancellationToken))
        {
            this.taskCancellationToken = taskCancellationToken;
        }

        /// <summary>
        ///     Supports multi-threaded producers.
        ///     Time complexity: O(1).
        /// </summary>
        internal async Task EnqueueAsync(T value)
        {
            await consumerQueueLock.WaitAsync();

            if(consumerQueue.Count > 0)
            {
                var consumer = consumerQueue.Dequeue();
                consumer.SetResult(value);
            }
            else
            {
                queue.Enqueue(value);
            }

            consumerQueueLock.Release();
        }

        /// <summary>
        ///      Supports multi-threaded consumers.
        ///      Time complexity: O(1).
        /// </summary>
        internal async Task<T> DequeueAsync()
        {
            await consumerQueueLock.WaitAsync();

            if (queue.Count > 0)
            {
                var result = queue.Dequeue();
                consumerQueueLock.Release();
                return result;
            }
          
            var consumer = new TaskCompletionSource<T>();
            taskCancellationToken.Register(() => consumer.TrySetCanceled());
            consumerQueue.Enqueue(consumer);
            
            consumerQueueLock.Release();

            return await consumer.Task;

        }
    }
}
