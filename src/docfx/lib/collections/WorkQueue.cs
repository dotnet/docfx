// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class WorkQueue<T> where T : notnull
    {
        private static readonly int s_maxParallelism = Math.Max(8, Environment.ProcessorCount * 2);

        private readonly ErrorLog _errorLog;
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly ConcurrentHashSet<T> _duplicationDetector = new ConcurrentHashSet<T>();

        private readonly TaskCompletionSource<int> _drainTcs = new TaskCompletionSource<int>();

        private volatile Action<T>? _run;

        // For progress reporting
        private int _totalCount = 0;
        private int _processedCount = 0;

        // For completion detection
        private int _remainingCount = 1;

        // When an exception occurs, store it here,
        // then wait until all jobs to complete before Drain returns.
        // This ensures _run callback is never executed once Drain returns.
        private volatile Exception? _exception;

        // Limit parallelism so we don't starve the thread pool.
        private int _parallelism;

        private int _drained;

        public WorkQueue(ErrorLog errorLog)
        {
            _errorLog = errorLog;
        }

        public void Enqueue(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Enqueue(item);
            }
        }

        public void Enqueue(T item)
        {
            if (!_duplicationDetector.TryAdd(item) || _drainTcs.Task.IsCompleted)
            {
                return;
            }

            _queue.Enqueue(item);

            Interlocked.Increment(ref _totalCount);
            Interlocked.Increment(ref _remainingCount);

            DrainCore();
        }

        public void Start(Action<T> run)
        {
            if (Interlocked.CompareExchange(ref _run, run, null) != null)
            {
                throw new InvalidOperationException();
            }

            _run = run;
        }

        public void WaitForCompletion()
        {
            if (Interlocked.Exchange(ref _drained, 1) == 1)
            {
                throw new InvalidOperationException();
            }

            OnComplete();

            _drainTcs.Task.GetAwaiter().GetResult();
        }

        private void DrainCore()
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

                Interlocked.Increment(ref _parallelism);

                ThreadPool.QueueUserWorkItem(Run, item, preferLocal: true);
            }
        }

        private void Run(T item)
        {
            if (_exception != null)
            {
                OnComplete();
                return;
            }

            var run = _run ?? throw new InvalidOperationException();

            try
            {
                _run(item);
                OnComplete();
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                _errorLog.Write(dex);
                OnComplete();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing '{item}'");
                OnComplete(ex);
            }
        }

        private void OnComplete(Exception? exception = null)
        {
            Interlocked.Decrement(ref _parallelism);

            try
            {
                Progress.Update(Interlocked.Increment(ref _processedCount), _totalCount);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception != null)
            {
                _exception = exception;
            }

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
            else
            {
                DrainCore();
            }
        }
    }
}
