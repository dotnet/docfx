// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class JsonDiskCacheTest
    {
        [Fact]
        public static async Task JsonDiskCache_UpdateExpiredItems_Asynchroniously()
        {
            var counter = 0;
            var cache = new JsonDiskCache<string, TestCacheObject>($"jsondiskcache/{Guid.NewGuid()}", TimeSpan.FromHours(1));

            Assert.Equal(0, counter);

            // First call is a blocking call
            Assert.Equal(1, cache.GetOrAdd(9999, CreateValue).value.Snapshot);

            // Subsequent calls does not trigger valueFactory
            Assert.Equal(1, cache.GetOrAdd(9999, CreateValue).value.Snapshot);

            // When cache expires, don't block caller, trigger asynchronious update
            cache.GetOrAdd(9999, CreateValue).value.Expiry = DateTime.UtcNow.AddHours(-1);
            Assert.Equal(1, cache.GetOrAdd(9999, CreateValue).value.Snapshot);

            // Save waits for asynchronious update to complete
            await cache.Save();
            Assert.Equal(2, cache.GetOrAdd(9999, CreateValue).value.Snapshot);

            async Task<(string, TestCacheObject)> CreateValue(int id)
            {
                await Task.Yield();
                return (null, new TestCacheObject { Id = id, Snapshot = Interlocked.Increment(ref counter) });
            }
        }

        class TestCacheObject : ICacheObject
        {
            public int Id { get; set; }

            public int Snapshot { get; set; }

            public DateTime? Expiry { get; set; }

            public object[] GetKeys() => new object[] { Id };
        }
    }
}
