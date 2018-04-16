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

        public static Task ForEach<T>(IEnumerable<T> source, Func<T, Task> action)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Parallel run actions including their returned children actions
        /// </summary>
        /// <typeparam name="T">The source type</typeparam>
        /// <param name="source">The source</param>
        /// <param name="action">The action need to be run</param>
        /// <param name="progress">The progress total running actions</param>
        /// <param name="error">The error handler</param>
        /// <returns>The task status</returns>
        public static Task ForEach<T>(IEnumerable<T> source, Func<T, Task<IEnumerable<T>>> action, Action<int, int> progress = null, Action<Exception, T> error = null)
        {
            ActionBlock<T> queue = null;

            var total = 0;
            var done = 0;

            queue = new ActionBlock<T>(
                async item =>
                {
                    try
                    {
                        var childActions = await action(item);
                        foreach (var childAction in childActions)
                        {
                            Enqueue(childAction);
                        }
                    }
                    catch (Exception ex) when (error != null)
                    {
                        error(ex, item);
                    }
                    finally
                    {
                        var completed = Interlocked.Increment(ref done) == Volatile.Read(ref total);
                        if (completed)
                        {
                            queue.Complete();
                        }

                        progress?.Invoke(done, total);
                    }
                }, s_dataflowOptions);

            foreach (var item in source)
            {
                Enqueue(item);
            }

            if (total == 0)
            {
                queue.Complete();
            }

            return queue.Completion;

            void Enqueue(T item)
            {
                if (item == null)
                {
                    return;
                }

                var count = Interlocked.Increment(ref total);
                var posted = queue.Post(item);
                Debug.Assert(posted);

                progress?.Invoke(done, count);
            }
        }
    }
}
