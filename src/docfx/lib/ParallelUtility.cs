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
                DebugAssertPostedOrFaulted(posted, queue);

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
            ActionBlock<(T, bool)> queue = null;

            var done = 0;
            var total = 0;
            var running = 1; // Always run the virual root node

            queue = new ActionBlock<(T, bool)>(Run, s_dataflowOptions);

            // Enqueue a virtual root node as a placeholder,
            // it is responsible for queueing each items in source parameter
            queue.Post((default, true));
            return queue.Completion;

            async Task Run((T, bool) input)
            {
                var (item, root) = input;

                if (root)
                {
                    foreach (var sourceItem in source)
                    {
                        Enqueue(sourceItem);
                    }
                }
                else
                {
                    await action(item, Enqueue);
                }

                if (Interlocked.Decrement(ref running) == 0)
                {
                    queue.Complete();
                }

                progress?.Invoke(Interlocked.Increment(ref done), total);
            }

            void Enqueue(T item)
            {
                Debug.Assert(item != null);

                if (predicate == null || predicate(item))
                {
                    var posted = queue.Post((item, false));
                    DebugAssertPostedOrFaulted(posted, queue);

                    Interlocked.Increment(ref running);

                    progress?.Invoke(done, Interlocked.Increment(ref total));
                }
            }
        }

        [Conditional("Debug")]
        private static void DebugAssertPostedOrFaulted<T>(bool posted, ActionBlock<T> queue)
        {
            if (!posted)
            {
                try
                {
                    queue.Completion.Wait();
                }
                catch
                {
                }

                Debug.Assert(queue.Completion.IsFaulted);
            }
        }
    }
}
