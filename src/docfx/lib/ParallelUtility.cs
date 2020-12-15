// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.Docs.Build
{
    internal static class ParallelUtility
    {
        private static readonly int s_maxParallelism = Math.Max(8, Environment.ProcessorCount * 2);

        private static readonly ParallelOptions s_parallelOptions = new() { MaxDegreeOfParallelism = s_maxParallelism };
        private static readonly ExecutionDataflowBlockOptions s_dataflowOptions = new()
        {
            MaxDegreeOfParallelism = s_maxParallelism,
            BoundedCapacity = DataflowBlockOptions.Unbounded,
            EnsureOrdered = false,
        };

        public static void ForEach<T>(LogScope scope, ErrorBuilder errors, IEnumerable<T> source, Action<T> action)
        {
            var done = 0;
            var total = source.Count();

            Parallel.ForEach(source, s_parallelOptions, item =>
            {
                try
                {
                    action(item);
                }
                catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
                {
                    errors.AddRange(dex);
                }
                catch
                {
                    Console.WriteLine($"Error processing '{item}'");
                    throw;
                }

                Progress.Update(scope, Interlocked.Increment(ref done), total);
            });
        }

        public static async Task ForEach<T>(LogScope scope, ErrorLog errorLog, IEnumerable<T> source, Func<T, Task> action)
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

            try
            {
                await queue.Completion;
            }
            catch (WrapException we)
            {
                ExceptionDispatchInfo.Capture(we.CapturedException).Throw();
            }

            async Task Run(T item)
            {
                try
                {
                    await action(item);
                }
                catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
                {
                    errorLog.AddRange(dex);
                }
                catch (OperationCanceledException oce)
                {
                    // Action block catches cancellation exceptions
                    // Check file /System.Threading.Tasks.Dataflow/src/Blocks/ActionBlock.cs#L142 at
                    // https://github.com/dotnet/corefx/blob/4b36fba308d8e2d3207773952c30268ac3365eed/src
                    throw new WrapException(oce);
                }
                catch
                {
                    Console.WriteLine($"Error processing '{item}'");
                    throw;
                }

                Progress.Update(scope, Interlocked.Increment(ref done), total);
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

        private class WrapException : Exception
        {
            public Exception CapturedException { get; }

            public WrapException(Exception capturedException) => CapturedException = capturedException;
        }
    }
}
