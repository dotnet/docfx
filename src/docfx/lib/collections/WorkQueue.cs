// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class WorkQueue<T>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly ConcurrentHashSet<T> _recurseDetector = new ConcurrentHashSet<T>();

        private readonly TaskCompletionSource<int> _drainTcs = new TaskCompletionSource<int>();

        // For progress reporting
        private int _totalCount = 0;
        private int _processedCount = 0;

        // For completion detection
        private int _remainingCount = 0;

        public void Enqueue(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Enqueue(item);
            }
        }

        public void Enqueue(T item)
        {
            if (_drainTcs.Task.IsCompleted)
            {
                throw new InvalidOperationException();
            }

            if (!_recurseDetector.TryAdd(item))
            {
                return;
            }

            _queue.Enqueue(item);

            Interlocked.Increment(ref _totalCount);
            Interlocked.Increment(ref _remainingCount);
        }

        public Task Drain(Func<T, Task> run, Action<int, int> progress = null)
        {
            if (_queue.Count == 0)
            {
                return Task.CompletedTask;
            }

            DrainCore();
            return _drainTcs.Task;

            void DrainCore()
            {
                while (!_drainTcs.Task.IsCompleted && _queue.TryDequeue(out var item))
                {
                    ThreadPool.QueueUserWorkItem(Run, item, preferLocal: true);
                }
            }

            void Run(T item)
            {
                try
                {
                    run(item).ContinueWith(OnComplete, default, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }
                catch (Exception ex)
                {
                    _drainTcs.TrySetException(ex);
                }
            }

            void OnComplete(Task task)
            {
                try
                {
                    if (task.Exception != null)
                    {
                        _drainTcs.TrySetException(task.Exception.InnerException);
                    }
                    else if (Interlocked.Decrement(ref _remainingCount) == 0)
                    {
                        _drainTcs.TrySetResult(0);
                    }
                    else
                    {
                        DrainCore();
                    }

                    progress?.Invoke(Interlocked.Increment(ref _processedCount), _totalCount);
                }
                catch (Exception ex)
                {
                    _drainTcs.TrySetException(ex);
                }
            }
        }
    }
}
