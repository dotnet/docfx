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

        public static Task ForEach<T>(IEnumerable<T> source, Func<T, Task> action, Action<int, int> progress = null, Action<Exception, T> error = null)
        {
            var done = 0;
            var total = 0;
            var queue = new ActionBlock<T>(Run, s_dataflowOptions);

            foreach (var item in source)
            {
                var posted = queue.Post(item);
                Debug.Assert(posted);
                total++;
            }

            queue.Complete();
            return queue.Completion;

            async Task Run(T item)
            {
                try
                {
                    await action(item);
                }
                catch (Exception ex) when (error != null)
                {
                    error(ex, item);
                }
                finally
                {
                    progress?.Invoke(Interlocked.Increment(ref done), total);
                }
            }
        }
    }
}
