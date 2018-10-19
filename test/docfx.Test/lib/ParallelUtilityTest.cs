// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class ParallelUtilityTest
    {
        [Theory]
        [InlineData(typeof(InvalidOperationException))]
        [InlineData(typeof(OperationCanceledException))]
        [InlineData(typeof(TaskCanceledException))]
        public static async Task ThrowsTheSameException(Type exceptionType)
        {
            var exception = await Assert.ThrowsAnyAsync<Exception>(() => ParallelUtility.ForEach(Enumerable.Range(0, 1000), Run));
            Assert.Equal(exceptionType, exception.GetType());

            Task Run(int n) => n % 500 == 0 ? throw (Exception)Activator.CreateInstance(exceptionType) : Task.CompletedTask;
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

        [Fact]
        public static async Task MustRunSource()
        {
            var done = 0;
            var total = 10;
            await ParallelUtility.ForEach(Enumerable.Range(0, total), Run, null, (a, b) => Thread.Sleep(100));

            Task Run(int n, Action<int> queue)
            {
                Interlocked.Increment(ref done);
                return Task.CompletedTask;
            }

            Assert.Equal(total, done);
        }
    }
}
