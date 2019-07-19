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
        private static readonly int s_maxParallelism = Math.Max(8, Environment.ProcessorCount * 2);

        private readonly Func<T, Task> _run;
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly ConcurrentHashSet<T> _duplicationDetector = new ConcurrentHashSet<T>();

        private readonly TaskCompletionSource<int> _drainTcs = new TaskCompletionSource<int>();

        public WorkQueue(Func<T, Task> run)
        {
            _run = run;
        }

        // For progress reporting
        private int _totalCount = 0;
        private int _processedCount = 0;

        // For completion detection
        private int _remainingCount = 0;

        // When an exception occurs, store it here,
        // then wait until all jobs to complete before Drain returns.
        // This ensures _run callback is never executed once Drain returns.
        private volatile Exception _exception;

        // Limit parallelism so we don't starve the thread pool.
        private int _parallelism;

        public void Enqueue(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Enqueue(item);
            }
        }

        public void Enqueue(T item)
        {
            if (!_duplicationDetector.TryAdd(item))
            {
                return;
            }

            _queue.Enqueue(item);

            Interlocked.Increment(ref _totalCount);
            Interlocked.Increment(ref _remainingCount);
        }

        public Task Drain(Action<int, int> progress = null)
        {
            if (_queue.Count == 0)
            {
                return Task.CompletedTask;
            }

            DrainCore();
            return _drainTcs.Task;

            void DrainCore()
            {
                while (!_drainTcs.Task.IsCompleted)
                {
                    if (!_queue.TryPeek(out var item))
                    {
                        break;
                    }

                    if (Volatile.Read(ref _parallelism) > s_maxParallelism)
                    {
                        break;
                    }

                    if (!_queue.TryDequeue(out item))
                    {
                        break;
                    }

                    if (_exception != null)
                    {
                        OnComplete();
                        break;
                    }

                    Interlocked.Increment(ref _parallelism);

                    ThreadPool.QueueUserWorkItem(Run, item, preferLocal: true);
                }
            }

            void Run(T item)
            {
                if (_exception != null)
                {
                    OnComplete();
                    return;
                }

                Task task;

                try
                {
                    task = _run(item);
                }
                catch (Exception ex)
                {
                    OnComplete(ex);
                    return;
                }

                task.ContinueWith(
                    t => OnComplete(t.Exception?.InnerException),
                    default,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            void OnComplete(Exception exception = null)
            {
                Interlocked.Decrement(ref _parallelism);

                try
                {
                    progress?.Invoke(Interlocked.Increment(ref _processedCount), _totalCount);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                if (exception != null)
                {
                    _exception = exception;
                }

                DrainCore();

                if (Interlocked.Decrement(ref _remainingCount) == 0)
                {
                    if (_exception is null)
                    {
                        _drainTcs.SetResult(0);
                    }
                    else
                    {
                        _drainTcs.SetException(_exception);
                    }
                }
            }
        }
    }
}
