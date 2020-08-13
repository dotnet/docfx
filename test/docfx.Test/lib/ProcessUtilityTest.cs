// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class ProcessUtilityTest
    {
        static ProcessUtilityTest()
        {
            Directory.CreateDirectory("process-test");
        }

        [Fact]
        public static void RunCommandsInParallel()
        {
            var cwd = GitUtility.FindRepository(Path.GetFullPath("README.md"));

            Parallel.For(0, 10, i => Assert.NotEmpty(ProcessUtility.Execute("git", "rev-parse HEAD", cwd)));
        }

        [Fact]
        public static void ExeNotFoundMessage()
        {
            var ex = Assert.Throws<Win32Exception>(() => Process.Start("a-fake-exe"));
            Assert.True(ProcessUtility.IsExeNotFoundException(ex), ex.ErrorCode + " " + ex.NativeErrorCode + " " + ex.Message);
        }

        [Fact]
        public static void SanitizeErrorMessage()
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => ProcessUtility.Execute("git", "rev-pa", secrets: new[] { "rev" }));

            Assert.DoesNotContain("rev", ex.Message);
            Assert.Contains("***", ex.Message);
        }

        [Fact]
        public static void InterProcessMutexTest()
        {
            var concurrencyLevel = 0;
            var fileName = $"process-test/{Guid.NewGuid()}";

            try
            {
                Parallel.ForEach(Enumerable.Range(0, 5), _ =>
                {
                    using (InterProcessMutex.Create(fileName))
                    {
                        Assert.Equal(1, Interlocked.Increment(ref concurrencyLevel));
                        Thread.Sleep(100);
                        Assert.Equal(0, Interlocked.Decrement(ref concurrencyLevel));
                    }
                });
            }
            catch (Exception ex)
            {
                Assert.True(false, ex.HResult + " " + ex.Message);
            }
        }

        [Fact]
        public static void NestedRunInMutexWithDifferentNameTest()
        {
            // nested run works for different names
            using (InterProcessMutex.Create($"process-test/{Guid.NewGuid()}"))
            using (InterProcessMutex.Create($"process-test/{Guid.NewGuid()}"))
            {
                // do nothing
            }
        }

        [Fact]
        public static void NestedRunInMutexWithSameNameTest()
        {
            // nested run doesn't work for same lock name
            Assert.ThrowsAny<Exception>(() =>
            {
                var name = Guid.NewGuid().ToString();
                using (InterProcessMutex.Create($"process-test/{name}"))
                using (InterProcessMutex.Create($"process-test/{Guid.NewGuid()}"))
                using (InterProcessMutex.Create($"process-test/{name}"))
                {
                    // do nothing
                }
            });
        }

        [Fact]
        public static void ParallelNestedRunInMutexWithSameNameTest()
        {
            var name = Guid.NewGuid().ToString();

            using (InterProcessMutex.Create($"process-test/123"))
            {
                Parallel.ForEach(new[] { 1, 2, 3, 4, 5 }, i =>
                {
                    using (InterProcessMutex.Create($"process-test/{name}"))
                    {
                        // do nothing
                    }
                });
            }
        }

        [Fact]
        public static void WritersAreMutuallyExclusiveFromReaders()
        {
            var name = Guid.NewGuid().ToString();
            var read = false;

            using var barrier = new Barrier(2);
            Task.WaitAll(
                Task.Run(() =>
                {
                    using (InterProcessReaderWriterLock.CreateWriterLock(name))
                    {
                        barrier.SignalAndWait();
                        Thread.Sleep(500);
                        Assert.False(read);
                    }
                }),
                Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    using (InterProcessReaderWriterLock.CreateReaderLock(name))
                    {
                        read = true;
                    }
                }));

            Assert.True(read);
        }

        [Fact]
        public static void WritersAreMutuallyExclusiveFromWriters()
        {
            var name = Guid.NewGuid().ToString();
            var write = false;

            using var barrier = new Barrier(2);
            Task.WaitAll(
                Task.Run(() =>
                {
                    using (InterProcessReaderWriterLock.CreateWriterLock(name))
                    {
                        barrier.SignalAndWait();
                        Thread.Sleep(500);
                        Assert.False(write);
                    }
                }),
                Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    using (InterProcessReaderWriterLock.CreateWriterLock(name))
                    {
                        write = true;
                    }
                }));

            Assert.True(write);
        }

        [Fact]
        public static void ReadersMayBeConcurrent()
        {
            var name = Guid.NewGuid().ToString();
            var count = 0;

            using var barrier = new Barrier(2);
            Assert.Equal(0, count);
            Task.WaitAll(
                Task.Run(() =>
                {
                    using (InterProcessReaderWriterLock.CreateReaderLock(name))
                    {
                        count++;
                        Assert.Equal(1, count);
                        barrier.SignalAndWait(); // 1
                        barrier.SignalAndWait(); // 2
                        Assert.Equal(2, count);
                    }
                }),
                Task.Run(() =>
                {
                    barrier.SignalAndWait(); // 1
                    using (InterProcessReaderWriterLock.CreateReaderLock(name))
                    {
                        count++;
                        barrier.SignalAndWait(); // 2
                        Assert.Equal(2, count);
                    }
                }));
        }
    }
}
