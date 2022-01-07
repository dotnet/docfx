// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build;

public static class JsonDiskCacheTest
{
    [Fact]
    public static void JsonDiskCache_BlockUpdate_MissingItems()
    {
        var counter = 0;
        var cache = new JsonDiskCache<string, int, TestCacheObject>($"jsondiskcache/{Guid.NewGuid()}", TimeSpan.FromHours(1));

        Assert.Equal(0, counter);

        // First call is a blocking call
        Assert.Equal(1, cache.GetOrAdd(9999, CreateValue).value.Snapshot);

        // Subsequent calls does not trigger valueFactory
        Assert.Equal(1, cache.GetOrAdd(9999, CreateValue).value.Snapshot);

        async Task<(string, TestCacheObject)> CreateValue(int id)
        {
            await Task.Yield();
            return (null, new TestCacheObject { Id = id, Snapshot = Interlocked.Increment(ref counter) });
        }
    }

    [Fact]
    public static void JsonDiskCache_AsynchronousUpdate_ExpiredItems()
    {
        var counter = 0;
        var filename = $"jsondiskcache/{Guid.NewGuid()}";
        Directory.CreateDirectory(Path.GetDirectoryName(filename) ?? ".");
        File.WriteAllText(filename, "{'items':[{'id': 9999, 'snapshot': 1234}]}".Replace('\'', '"'));

        var cache = new JsonDiskCache<string, int, TestCacheObject>(filename, TimeSpan.FromHours(1));

        // Read existing items from cache does not trigger valueFactory
        Assert.Equal(1234, cache.GetOrAdd(9999, CreateValue).value.Snapshot);
        Assert.Equal(0, counter);

        // When cache expires, don't block caller, trigger asynchronous update
        cache.GetOrAdd(9999, CreateValue).value.UpdatedAt = DateTime.MinValue;
        Assert.Equal(1234, cache.GetOrAdd(9999, CreateValue).value.Snapshot);

        // Save waits for asynchronous update to complete
        cache.Save();
        Assert.Equal(1, cache.GetOrAdd(9999, CreateValue).value.Snapshot);

        async Task<(string, TestCacheObject)> CreateValue(int id)
        {
            await Task.Yield();
            return (null, new TestCacheObject { Id = id, Snapshot = Interlocked.Increment(ref counter) });
        }
    }

    private class TestCacheObject : ICacheObject<int>
    {
        public int Id { get; set; }

        public int Snapshot { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public IEnumerable<int> GetKeys() => new[] { Id };
    }
}
