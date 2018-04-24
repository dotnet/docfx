// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class ProcessUtilityTest
    {
        [Fact]
        public static async Task ConcurrencyCreatingFileShouldNotThrowNoException()
        {
            var fileName = $".process_test\\{Guid.NewGuid()}";
            await Task.WhenAll(Enumerable.Range(0, 5).AsParallel().Select(i => ProcessUtility.ProcessLock(
            () =>
            {
                using (var streamWriter = File.CreateText(fileName))
                {
                    streamWriter.WriteLine(fileName);
                }

                File.Delete(fileName);
                return Task.FromResult(0);
            }, Path.GetFullPath($"{fileName}.lock"))));
        }
    }
}
