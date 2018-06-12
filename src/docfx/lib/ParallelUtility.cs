// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.Docs.Build
{
    internal static class ParallelUtility
    {
        private static readonly ExecutionDataflowBlockOptions s_dataflowOptions = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
            BoundedCapacity = DataflowBlockOptions.Unbounded,
            EnsureOrdered = false,
        };

        public static Task ForEach<T>(IEnumerable<T> source, Func<T, Task> action, Action<int, int> progress = null)
        {
            var done = 0;
            var total = 0;
            var queue = new ActionBlock<T>(Run, s_dataflowOptions);

            foreach (var item in source)
            {
                var posted = queue.Post(item);

                // https://github.com/dotnet/corefx/issues/21715
                // Post on an ActionBlock that's unbounded should only return false
                // if the ActionBlock has been closed to additional messages, which could happen for example
                // if someone called Complete on the block
                // or if the block's delegate threw an exception that went unhandled and caused the block to fault.
                Debug.Assert(posted || queue.Completion.IsFaulted);
                total++;
            }

            queue.Complete();
            return queue.Completion;

            async Task Run(T item)
            {
                await action(item);
                progress?.Invoke(Interlocked.Increment(ref done), total);
            }
        }

        /// <summary>
        /// Parallel run actions including their returned children actions
        /// </summary>
        public static Task ForEach<T>(IEnumerable<T> source, Func<T, Action<T>, Task> action, Func<T, bool> predicate = null, Action<int, int> progress = null)
        {
            ActionBlock<T> queue = null;

            var total = 0;
            var done = 0;
            queue = new ActionBlock<T>(Run, s_dataflowOptions);

            foreach (var item in source)
            {
                Enqueue(item);
            }

            if (total == 0)
            {
                queue.Complete();
            }

            return queue.Completion;

            async Task Run(T item)
            {
                await action(item, Enqueue);
                var completed = Interlocked.Increment(ref done) == Volatile.Read(ref total);
                if (completed)
                {
                    queue.Complete();
                }

                progress?.Invoke(done, total);
            }

            void Enqueue(T item)
            {
                if (item == null)
                {
                    return;
                }
                if (predicate != null && !predicate(item))
                {
                    return;
                }

                var count = Interlocked.Increment(ref total);
                var posted = queue.Post(item);
                Debug.Assert(posted || queue.Completion.IsFaulted);

                progress?.Invoke(done, count);
            }
        }
    }
}
