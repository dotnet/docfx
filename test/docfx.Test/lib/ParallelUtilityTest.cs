// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class ParallelUtilityTest
    {
        [Fact]
        public static async Task ThrowsTheSameException()
        {
            for (var i = 0; i < 5; i++)
            {
                await Assert.ThrowsAsync<Exception>(() => ParallelUtility.ForEach(Enumerable.Range(0, 1000), Run));

                Task Run(int n) => n % 500 == 0 ? throw new Exception() : Task.CompletedTask;
            }
        }

        [Fact]
        public static async Task PostToActionBlockAlwaysSucceed()
        {
            await ParallelUtility.ForEach(Enumerable.Range(0, 10), Run);

            Task Run(int n, Action<int> queue)
            {
                for (var i = 0; i < n; i++)
                {
                    queue(i);
                }
                return Task.CompletedTask;
            }
        }
    }
}
