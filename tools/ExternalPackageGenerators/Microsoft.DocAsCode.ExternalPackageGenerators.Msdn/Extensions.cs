// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.ExternalPackageGenerators.Msdn
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    internal static class Extensions
    {
        public static IEnumerable<XmlReader> Elements(this XmlReader reader, string name)
        {
            reader.Read();
            while (reader.ReadToNextSibling(name))
            {
                using (var result = reader.ReadSubtree())
                {
                    result.Read();
                    yield return result;
                }
            }
        }

        public static IEnumerable<T> ProtectResource<T>(this IEnumerable<T> source)
            where T : IDisposable
        {
            foreach (var item in source)
            {
                using (item)
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<T> EmptyIfThrow<T>(this Func<T> func)
        {
            try
            {
                return new[] { func() };
            }
            catch (Exception)
            {
                return Enumerable.Empty<T>();
            }
        }

        public static IEnumerable<T> AcquireSemaphore<T>(this IEnumerable<T> source, SemaphoreSlim semaphore)
        {
            foreach (var item in source)
            {
                semaphore.Wait();
                yield return item;
            }
        }

        public static IEnumerable<List<T>> BlockBuffer<T>(this IEnumerable<T> source, Func<int> getBlockSize)
        {
            var blockSize = getBlockSize();
            if (blockSize <= 0)
            {
                blockSize = 1;
            }
            var list = new List<T>(blockSize);
            foreach (var item in source)
            {
                list.Add(item);
                if (list.Count == blockSize)
                {
                    yield return list;
                    blockSize = getBlockSize();
                    if (blockSize <= 0)
                    {
                        blockSize = 1;
                    }
                    list = new List<T>(blockSize);
                }
            }
        }

        public static async Task<HttpResponseMessage> GetWithRetryAsync(this HttpClient client, string url, SemaphoreSlim semaphore, params int[] retryDelay)
        {
            if (retryDelay.Any(delay => delay <= 0))
            {
                throw new ArgumentException("Delay should be greate than 0.", nameof(retryDelay));
            }
            await semaphore.WaitAsync();
            try
            {
                int retryCount = 0;
                while (true)
                {
                    try
                    {
                        return await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    }
                    catch (TaskCanceledException)
                    {
                        if (retryCount >= retryDelay.Length)
                        {
                            throw;
                        }
                    }
                    await Task.Delay(retryDelay[retryCount]);
                    retryCount++;
                }
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
