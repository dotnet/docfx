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
        [Fact]
        public static void ExeNotFoundMessage()
        {
            var ex = Assert.Throws<Win32Exception>(() => Process.Start("a-fake-exe"));
            Assert.True(ProcessUtility.IsNotFound(ex), ex.ErrorCode + " " + ex.NativeErrorCode + " " + ex.Message);
        }

        [Fact]
        public static async Task CreateFilesInMutexInParallelDoesNotThrow()
        {
            var fileName = $"process-test\\{Guid.NewGuid()}";
            await Task.WhenAll(Enumerable.Range(0, 5).AsParallel().Select(
                i => ProcessUtility.RunInMutex(
                    fileName,
                    () =>
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(fileName)));
                        using (var streamWriter = File.CreateText(fileName))
                        {
                            Thread.Sleep(100);
                            streamWriter.WriteLine(fileName);
                        }

                        File.Delete(fileName);
                        return Task.FromResult(0);
                    })));
        }
    }
}
