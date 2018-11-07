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
        public static async Task RunCommandsInParallel()
        {
            var cwd = GitUtility.FindRepo(Path.GetFullPath("README.md"));

            var results = await Task.WhenAll(Enumerable.Range(0, 10).AsParallel().Select(i => ProcessUtility.Execute("git", "rev-parse HEAD", cwd)));

            Assert.True(results.All(r => !string.IsNullOrEmpty(r.stdout)));
        }

        [Fact]
        public static void ExeNotFoundMessage()
        {
            var ex = Assert.Throws<Win32Exception>(() => Process.Start("a-fake-exe"));
            Assert.True(ProcessUtility.IsExeNotFoundException(ex), ex.ErrorCode + " " + ex.NativeErrorCode + " " + ex.Message);
        }

        [Fact]
        public static async Task FileMutexTest()
        {
            var concurrencyLevel = 0;
            var fileName = $"process-test/{Guid.NewGuid()}";

            try
            {
                await ParallelUtility.ForEach(
                    Enumerable.Range(0, 5),
                    i => ProcessUtility.RunInsideMutex(
                        fileName,
                        async () =>
                        {
                            Assert.Equal(1, Interlocked.Increment(ref concurrencyLevel));
                            await Task.Delay(100);
                            Assert.Equal(0, Interlocked.Decrement(ref concurrencyLevel));
                        }));
            }
            catch (Exception ex)
            {
                Assert.True(false, ex.HResult + " " + ex.Message);
            }
        }

        [Fact]
        public static async Task NestedRunInMutexTest()
        {
            // works for one lock one file
            await ProcessUtility.RunInsideMutex($"process-test/{Guid.NewGuid()}", async () => { await Task.Delay(100); });
            await ProcessUtility.RunInsideMutex($"process-test/{Guid.NewGuid()}", async () => { await Task.Delay(100); });

            // doesn't work for requiring a lock before releasing a lock
            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                await ProcessUtility.RunInsideMutex($"process-test/{Guid.NewGuid()}",
                        async () =>
                        {
                            await ProcessUtility.RunInsideMutex($"process-test/{Guid.NewGuid()}",
                                async () =>
                                {
                                    await Task.Delay(100);
                                });
                        });
            });
        }
    }
}
