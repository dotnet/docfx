// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
            // nested run works for different names
            await ProcessUtility.RunInsideMutex($"process-test/{Guid.NewGuid()}",
                async () =>
                {
                    await ProcessUtility.RunInsideMutex($"process-test/{Guid.NewGuid()}",
                        async () =>
                        {
                            await Task.Delay(100);
                        });
                });

            // nested run doesn't work for sanme lock name
            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                var name = Guid.NewGuid().ToString();
                await ProcessUtility.RunInsideMutex($"process-test/{name}",
                    async () =>
                    {
                        await ProcessUtility.RunInsideMutex($"process-test/{Guid.NewGuid()}",
                            async () =>
                            {
                                await ProcessUtility.RunInsideMutex($"process-test/{name}",
                                    async () =>
                                    {
                                        await Task.Delay(100);
                                    });
                            });
                    });
            });
        }

        [Theory]
        [InlineData(new[] { "a-s:a", "r-s:a" }, new[] { true, true })]
        [InlineData(new[] { "a-s:a", "r-s:a", "a-s:a", "r-s:a" }, new[] { true, true, true, true })]
        [InlineData(new[] { "a-s:a", "a-s:a", "r-s:a", "r-s:a" }, new[] { true, true, true, true })]
        [InlineData(new[] { "a-s:a", "r-s:a", "r-s:a" }, new[] { true, true, false })]
        [InlineData(new[] { "r-s:a" }, new[] { false })]
        [InlineData(new[] { "r-s:a", "a-s:a", "r-s:a" }, new[] { false, true, true })]
        [InlineData(new[] { "a-s:a", "a-s:b", "r-s:b", "r-s:a" }, new[] { true, true, true, true })]
        [InlineData(new[] { "a-s:a", "a-s:b", "r-s:a", "r-s:b" }, new[] { true, true, true, true })]
        [InlineData(new[] { "a-s:a", "r-s:a", "a-s:b", "r-s:b" }, new[] { true, true, true, true })]


        [InlineData(new[] { "a-e:a", "r-e:a" }, new[] { true, true })]
        [InlineData(new[] { "a-e:a", "r-e:a", "r-e:a" }, new[] { true, true, false })]
        [InlineData(new[] { "a-e:a", "a-e:a", "r-e:a" }, new[] { true, false, true })]
        [InlineData(new[] { "r-e:a" }, new[] { false })]
        [InlineData(new[] { "r-e:a", "a-e:a", "r-e:a" }, new[] { false, true, true })]
        [InlineData(new[] { "a-e:a", "r-e:a", "a-e:b", "r-e:b" }, new[] { true, true, true, true })]
        [InlineData(new[] { "a-e:a", "a-e:b", "r-e:a", "r-e:b" }, new[] { true, true, true, true })]
        [InlineData(new[] { "a-e:a", "a-e:b", "r-e:b", "r-e:a" }, new[] { true, true, true, true })]


        [InlineData(new[] { "a-s:a", "r-s:a", "a-e:a", "r-e:a" }, new[] { true, true, true, true })]
        [InlineData(new[] { "a-e:a", "r-e:a", "a-s:a", "r-s:a" }, new[] { true, true, true, true })]
        [InlineData(new[] { "a-s:a", "a-e:a", "r-s:a", "r-e:a" }, new[] { true, false, true, false })]
        [InlineData(new[] { "a-e:a", "a-s:a", "r-s:a", "r-e:a" }, new[] { true, false, false, true })]
        [InlineData(new[] { "a-s:a", "a-e:b", "r-s:a", "r-e:b" }, new[] { true, true, true, true })]

        public static async Task RunSharedAndExclusiveLock(string[] steps, bool[] results)
        {
            Debug.Assert(steps != null);
            Debug.Assert(results != null);
            Debug.Assert(steps.Length == results.Length);
            var guid = Guid.NewGuid().ToString();

            int i = 0;
            var acquirers = new Dictionary<string, List<string>>();

            foreach(var step in steps)
            {
                await Task.Yield();
                var parts = step.Split(new[] { ':' });
                Debug.Assert(parts.Length == 2);
                string acquirer = null;
                bool acquired = false;
                switch (parts[0])
                {
                    case "a-s": // acquire shared lock
                        (acquired, acquirer) = await ProcessUtility.AcquireSharedLock(guid + parts[1]);
                        Debug.Assert(acquired == results[i], $"{i}");
                        if (acquired)
                        {
                            var key = "s" + parts[1];
                            if (!acquirers.TryGetValue(key, out var list))
                            {
                                acquirers[key] = list = new List<string>();
                            }
                            list.Add(acquirer);
                        }
                        break;
                    case "a-e": // acquire exclusive lock
                        (acquired, acquirer) = await ProcessUtility.AcquireExclusiveLock(guid + parts[1]);
                        Debug.Assert(acquired == results[i], $"{i}");
                        if (acquired)
                        {
                            var key = "e" + parts[1];
                            if (!acquirers.TryGetValue(key, out var list))
                            {
                                acquirers[key] = list = new List<string>();
                            }
                            list.Add(acquirer);

                            Debug.Assert(list.Count == 1, $"{i}");
                        }
                        break;
                    case "r-s": // release shared lock
                        string sharedAcquirer = null;
                        if (acquirers.TryGetValue("s" + parts[1], out var sharedList))
                        {
                            Debug.Assert(sharedList.Count >= 1, $"{i}");
                            sharedAcquirer = sharedList[sharedList.Count - 1];
                            sharedList.RemoveAt(sharedList.Count - 1);
                            if (!sharedList.Any())
                            {
                                acquirers.Remove("s" + parts[1]);
                            }
                        }
                        var sharedReleased = await ProcessUtility.ReleaseSharedLock(guid + parts[1], sharedAcquirer ?? "not exists");
                        Debug.Assert(sharedReleased == results[i], $"{i}");
                        break;
                    case "r-e": // release exclusive lock
                        string exclusiveAcquirer = null;
                        if (acquirers.TryGetValue("e" + parts[1], out var exclusiveList))
                        {
                            Debug.Assert(exclusiveList.Count == 1, $"{i}");
                            exclusiveAcquirer = exclusiveList[exclusiveList.Count - 1];
                            exclusiveList.RemoveAt(exclusiveList.Count - 1);
                            acquirers.Remove("e" + parts[1]);
                        }
                        var exclusivedReleased = await ProcessUtility.ReleaseExclusiveLock(guid + parts[1], exclusiveAcquirer ?? "not exists");
                        Debug.Assert(exclusivedReleased == results[i], $"{i}");
                        break;
                }

                i++;
            }
        }
    }
}
