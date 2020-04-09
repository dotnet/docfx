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
        public static void ThrowsTheSameException(Type exceptionType)
        {
            var queue = new WorkQueue<int>();
            queue.Enqueue(Enumerable.Range(0, 1000));

            var exception = Assert.ThrowsAny<Exception>(() => queue.Drain(new ErrorLog(), Run));
            Assert.Equal(exceptionType, exception.GetType());

            void Run(int n)
            {
                if (n % 500 == 0)
                    throw (Exception)Activator.CreateInstance(exceptionType);
            }
        }

        [Fact]
        public static void MustRunSource()
        {
            var done = 0;
            var total = 10;

            var queue = new WorkQueue<int>();
            queue.Enqueue(Enumerable.Range(0, total));

            queue.Drain(new ErrorLog(), Run);

            void Run(int n)
            {
                Thread.Sleep(100);
                Interlocked.Increment(ref done);
            }

            Assert.Equal(total, done);
        }
    }
}
