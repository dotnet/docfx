// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class ProcessUtilityTest
    {
        [Fact]
        public static void ExeNotFoundMessage()
        {
            var ex = Assert.ThrowsAny<Exception>(() => Process.Start("a-fake-exe"));
            Assert.True(ProcessUtility.IsNotFound(ex), ex.Message);
        }

        [Fact]
        public static async Task ConcurrencyCreatingFileShouldNotThrowNoException()
        {
            var fileName = $".process_test\\{Guid.NewGuid()}";
            await Task.WhenAll(Enumerable.Range(0, 5).AsParallel().Select(
                i => ProcessUtility.ProcessLock(
                    $"{fileName}.lock",
                    () =>
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(fileName)));
                        using (var streamWriter = File.CreateText(fileName))
                        {
                            streamWriter.WriteLine(fileName);
                        }

                        File.Delete(fileName);
                        return Task.FromResult(0);
                    })));
        }
    }
}
