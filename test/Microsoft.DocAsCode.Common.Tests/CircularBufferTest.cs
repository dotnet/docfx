// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Threading.Tasks;
    using Xunit;

    using Microsoft.DocAsCode.Common;

    [Trait("Owner", "vwxyzh")]
    [Trait("Related", "CircularBuffer")]
    public class CircularBufferTest
    {
        [Fact]
        public void TestCircularBuffer()
        {
            var cb = new CircularBuffer<int>(2);
            Assert.Throws<InvalidOperationException>(() => cb.Read());
            cb.Write(1);
            Assert.Equal(1, cb.Read());
            cb.Write(2);
            cb.Write(3);
            Assert.Equal(2, cb.Read());
            cb.Write(4);
            Assert.Equal(new[] { 3, 4 }, Read(cb, 2));
            cb.Write(new[] { 5, 6, 7, 8, 9 });
            Assert.Equal(new[] { 5, 6, 7 }, Read(cb, 3));
            cb.Write(new[] { 10, 11 });
            Assert.Equal(8, cb.Read());
            cb.Write(new[] { 12, 13, 14, 15, 16, 17 });
            Assert.Equal(new[] { 9, 10, 11 }, Read(cb, 3));
            Assert.Equal(new[] { 12, 13, 14, 15, 16, 17 }, Read(cb, 6));
        }

        [Fact]
        public void TestCircularBuffer_2()
        {
            var cb = new CircularBuffer<int>(3);
            Assert.Equal(0, cb.Read(new int[10], 0, 10));
            cb.Write(new[] { 1, 2 });
            Assert.Equal(new[] { 1, 2 }, Read(cb, 2));
            cb.Write(new[] { 3, 4 });
            Assert.Equal(new[] { 3, 4 }, Read(cb, 2));
        }

        private static int[] Read(CircularBuffer<int> cb, int count)
        {
            var buffer = new int[count];
            int read = 0;
            while (read < count)
            {
                read += cb.Read(buffer, read, count - read);
            }
            return buffer;
        }

        [Fact]
        public void TestCircularStream()
        {
            const int LineCount = 100;
            var cs = new CircularStream();
            Task.Run(() =>
            {
                using (var writer = cs.CreateWriterView())
                using (var sw = new StreamWriter(writer))
                {
                    for (int i = 0; i < LineCount; i++)
                    {
                        sw.WriteLine($"Line {i}: test!");
                    }
                }
            });
            using (var reader = cs.CreateReaderView())
            using (var sr = new StreamReader(reader))
            {
                for (int i = 0; i < LineCount; i++)
                {
                    Assert.Equal($"Line {i}: test!", sr.ReadLine());
                }
                Assert.Equal(string.Empty, sr.ReadToEnd());
            }
        }

        [Fact]
        public async Task TestCompositeStream()
        {
            const int LineCount = 100;
            var ms = new MemoryStream();
            var cs = new CircularStream();
            var task = Task.Run(() =>
            {
                using (var stream = cs.CreateReaderView())
                {
                    return SHA1.Create().ComputeHash(stream);
                }
            });
            byte[] expected;
            using (var ds = new CompositeStream(ms, cs.CreateWriterView()))
            {
                var sw = new StreamWriter(ds);
                for (int i = 0; i < LineCount; i++)
                {
                    sw.WriteLine($"Line {i}: test!");
                }
                sw.Flush();
                expected = SHA1.Create().ComputeHash(ms.ToArray());
            }
            Assert.Equal(expected, await task);
        }
    }
}
