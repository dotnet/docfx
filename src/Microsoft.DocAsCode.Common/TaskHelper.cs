// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public static class TaskHelper
    {
        /// <summary>
        /// Provide parallel version for ForEach
        /// </summary>
        /// <typeparam name="T">The type for the enumerable</typeparam>
        /// <param name="source">The enumerable to control the foreach loop</param>
        /// <param name="body">The task body</param>
        /// <param name="maxParallelism">The max parallelism allowed</param>
        /// <returns>The task</returns>
        public static async Task ForEachInParallelAsync<T>(this IEnumerable<T> source, Func<T, Task> body, int maxParallelism)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            using (var semaphore = new SemaphoreSlim(maxParallelism))
            {
                // warning "access to disposed closure" around "semaphore" could be ignored as it is inside Task.WhenAll
                await Task.WhenAll(from s in source select ForEachCoreAsync(body, semaphore, s));
            }
        }

        private static async Task ForEachCoreAsync<T>(Func<T, Task> body, SemaphoreSlim semaphore, T s)
        {
            await semaphore.WaitAsync();
            try
            {
                await body(s);
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Provide parallel version for ForEach
        /// </summary>
        /// <typeparam name="T">The type for the enumerable</typeparam>
        /// <param name="source">The enumerable to control the foreach loop</param>
        /// <param name="body">The task body</param>
        /// <returns>The task</returns>
        /// <remarks>The max parallelism is 64.</remarks>
        public static Task ForEachInParallelAsync<T>(this IEnumerable<T> source, Func<T, Task> body)
        {
            return ForEachInParallelAsync(source, body, 64);
        }

        /// <summary>
        /// Provide parallel version for Select that each element will map to a result
        /// </summary>
        /// <typeparam name="TSource">The type for the enumerable</typeparam>
        /// <typeparam name="TResult">The type for the result</typeparam>
        /// <param name="source">The enumerable to control the select</param>
        /// <param name="body">The select body</param>
        /// <param name="maxParallelism">The max parallelism allowed</param>
        /// <returns>The task</returns>
        public static async Task<IReadOnlyList<TResult>> SelectInParallelAsync<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, Task<TResult>> body, int maxParallelism)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            using (var semaphore = new SemaphoreSlim(maxParallelism))
            {
                // warning "access to disposed closure" around "semaphore" could be ignored as it is inside Task.WhenAll
                return await Task.WhenAll(from s in source select SelectCoreAsync(body, semaphore, s));
            }
        }

        private static async Task<TResult> SelectCoreAsync<TSource, TResult>(Func<TSource, Task<TResult>> body, SemaphoreSlim semaphore, TSource s)
        {
            await semaphore.WaitAsync();
            try
            {
                return await body(s);
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Provide parallel version for Select that each element will map to a result
        /// </summary>
        /// <typeparam name="TSource">The type for the enumerable</typeparam>
        /// <typeparam name="TResult">The type for the result</typeparam>
        /// <param name="source">The enumerable to control the select</param>
        /// <param name="body">The select body</param>
        /// <returns>The task</returns>
        /// <remarks>The max parallelism is 64.</remarks>
        public static Task<IReadOnlyList<TResult>> SelectInParallelAsync<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, Task<TResult>> body)
        {
            return SelectInParallelAsync(source, body, 64);
        }

        /// <summary>
        /// A completed task
        /// </summary>
        public static readonly Task Completed = Task.FromResult(1);
    }
}
