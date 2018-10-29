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
        public static void ExeNotFoundMessage()
        {
            var ex = Assert.Throws<Win32Exception>(() => Process.Start("a-fake-exe"));
            Assert.True(ProcessUtility.IsExeNotFoundException(ex), ex.ErrorCode + " " + ex.NativeErrorCode + " " + ex.Message);
        }

        [Fact]
        public static void FileUsedByAnotherProcessMessage()
        {
            var path = $"process-test/{Guid.NewGuid()}";
            var content = Guid.NewGuid().ToString();
            File.WriteAllText(path, content);

            using (Read())
            {
                using (Read()) { }
                var ex = Assert.Throws<IOException>(Write);
                Assert.True(ProcessUtility.IsFileUsedByAnotherProcessException(ex), ex.HResult + " " + ex.Message);
            }

            using (Write())
            {
                Assert.True(ProcessUtility.IsFileUsedByAnotherProcessException(Assert.Throws<IOException>(Read)));
                Assert.True(ProcessUtility.IsFileUsedByAnotherProcessException(Assert.Throws<IOException>(Write)));
            }

            using (Read()) { }

            Stream Read() => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            Stream Write() => new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
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
    }
}
