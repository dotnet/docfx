// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <summary>
/// The utility class for docascode project
/// </summary>
namespace Microsoft.DocAsCode.Utility
{
    using System;
    using System.Linq;
    using System.IO;
    using System.ComponentModel;
    using System.Globalization;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Collections.Concurrent;

    public static class TaskHelper
    {
        /// <summary>
        /// Task.WhenAll, and re-throw AggregateException containing exceptions from all failed tasks
        /// </summary>
        /// <typeparam name="TResult">task result type</typeparam>
        /// <param name="tasks">the list of tasks</param>
        /// <returns>arrry of task result</returns>
        /// <exception>AggregationException of all failed tasks</exception>
        public static async Task<TResult[]> WhenAllAndThrowAggregateExceptionOnErrorAsync<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            if (tasks == null)
            {
                throw new ArgumentNullException("tasks");
            }

            Task<TResult[]> whenAllTask = null;
            try
            {
                whenAllTask = Task.WhenAll(tasks);
                return await whenAllTask;
            }
            catch
            {
                throw whenAllTask.Exception;
            }
        }

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
                throw new ArgumentNullException("body");
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
                throw new ArgumentNullException("body");
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

        public static async Task<T> FirstOrDefaultAsync<T>(this IEnumerable<T> source, Func<T, Task<bool>> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            foreach (var item in source)
            {
                if (await predicate(item))
                {
                    return item;
                }
            }
            return default(T);
        }

        public static async Task<T> FirstAsync<T>(this IEnumerable<T> source, Func<T, Task<bool>> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            foreach (var item in source)
            {
                if (await predicate(item))
                {
                    return item;
                }
            }
            throw new InvalidOperationException("Sequence contains no matching element.");
        }

        public static async Task<IEnumerable<T>> WhereAsync<T>(this IReadOnlyList<T> source, Func<T, Task<bool>> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            var conditions = await source.SelectInParallelAsync(predicate);
            return source.Where((x, i) => conditions[i]);
        }

        public static Task<IEnumerable<T>> WhereAsync<T>(this IEnumerable<T> source, Func<T, Task<bool>> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            return WhereAsync(source.ToList(), predicate);
        }

        public static async Task<Dictionary<TKey, TValue>> ToDictionaryAsync<T, TKey, TValue>(this IReadOnlyList<T> source, Func<T, TKey> keySelector, Func<T, Task<TValue>> valueSelector)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (keySelector == null)
            {
                throw new ArgumentNullException("keySelector");
            }
            if (valueSelector == null)
            {
                throw new ArgumentNullException("valueSelector");
            }
            var values = await source.SelectInParallelAsync(valueSelector);
            var result = new Dictionary<TKey, TValue>();
            for (int i = 0; i < source.Count; i++)
            {
                result.Add(keySelector(source[i]), values[i]);
            }
            return result;
        }

        public static Task<Dictionary<TKey, TValue>> ToDictionaryAsync<T, TKey, TValue>(this IEnumerable<T> source, Func<T, TKey> keySelector, Func<T, Task<TValue>> valueSelector)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (keySelector == null)
            {
                throw new ArgumentNullException("keySelector");
            }
            if (valueSelector == null)
            {
                throw new ArgumentNullException("valueSelector");
            }
            return ToDictionaryAsync(source.ToList(), keySelector, valueSelector);
        }
    }

    /// <summary>
    /// An asynchronous wrapper over concurrent dictionary
    /// </summary>
    /// <typeparam name="TKey">Type of keys</typeparam>
    /// <typeparam name="TValue">Type of values</typeparam>
    /// <remarks>Implementation copied from http://msdn.microsoft.com/en-us/library/hh873173(v=vs.110).aspx, AsyncCache</remarks>
    public class AsyncConcurrentCache<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, Lazy<Task<TValue>>> _cache;

        public AsyncConcurrentCache(IEqualityComparer<TKey> comparer = null)
        {
            this._cache = comparer == null
                ? new ConcurrentDictionary<TKey, Lazy<Task<TValue>>>()
                : new ConcurrentDictionary<TKey, Lazy<Task<TValue>>>(comparer);
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="AsyncConcurrentCache{TKey,TValue}"/> by using the specified function, if the key does not already exist.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="valueFactory">The function used to get a task to generate value for the key</param>
        /// <param name="removeKeyOnFaulted">A flag indicating whether to remove the key from cache on faulted.</param>
        /// <returns>The task to generate value for the key</returns>
        public Task<TValue> GetOrAdd(TKey key, Func<TKey, Task<TValue>> valueFactory, bool removeKeyOnFaulted = true)
        {
            return this._cache.GetOrAdd(key, k => new Lazy<Task<TValue>>(() =>
            {
                Task<TValue> task = valueFactory(k);

                if (removeKeyOnFaulted)
                {
                    task.ContinueWith(task1 =>
                    {
                        Lazy<Task<TValue>> useless;
                        this._cache.TryRemove(key, out useless);
                    }, TaskContinuationOptions.OnlyOnFaulted);
                }

                return task;
            })).Value;
        }

        /// <summary>
        /// Attempts to get the value associated with the specified key from the ConcurrentDictionary<TKey, Lazy<Task<TValue>>>.
        /// </summary>
        /// <param name="key">The key of the element</param>
        /// <param name="value">The task to generate value for the key</param>
        /// <returns>true if the key was found in the ConcurrentDictionary<TKey, Lazy<Task<TValue>>>; otherwise, false.</returns>
        public bool TryGetValue(TKey key, out Task<TValue> value)
        {
            Lazy<Task<TValue>> lazyValue;
            var result = this._cache.TryGetValue(key, out lazyValue);

            value = null;
            if (lazyValue != null)
            {
                value = lazyValue.Value;
            }

            return result;
        }

        /// <summary>
        /// Gets a List containing the values in the ConcurrentDictionary<TKey, Lazy<Task<TValue>>>.
        /// </summary>
        public List<Task<TValue>> Values
        {
            get
            {
                return this._cache.Values.Select(x => x.Value).ToList();
            }
        }
    }
}
