// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SharedCode
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
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
            if (list.Count > 0)
            {
                yield return list;
            }
        }
    }
}
