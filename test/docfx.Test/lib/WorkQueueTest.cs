// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class WorkQueueTest
    {
        [Theory]
        [InlineData(typeof(InvalidOperationException))]
        [InlineData(typeof(OperationCanceledException))]
        [InlineData(typeof(TaskCanceledException))]
        public static async Task ThrowsTheSameException(Type exceptionType)
        {
            var queue = new WorkQueue<int>(Run);
            queue.Enqueue(Enumerable.Range(0, 1000));

            var exception = await Assert.ThrowsAnyAsync<Exception>(() => queue.Drain());
            Assert.Equal(exceptionType, exception.GetType());

            Task Run(int n) => n % 500 == 0 ? throw (Exception)Activator.CreateInstance(exceptionType) : Task.CompletedTask;
        }

        [Fact]
        public static async Task MustRunSource()
        {
            var done = 0;
            var total = 10;

            var queue = new WorkQueue<int>(Run);
            queue.Enqueue(Enumerable.Range(0, total));

            await queue.Drain();

            Task Run(int n)
            {
                Thread.Sleep(100);
                Interlocked.Increment(ref done);
                return Task.CompletedTask;
            }

            Assert.Equal(total, done);
        }
    }
}
