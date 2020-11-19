// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class WatcherTest
    {
        [Fact]
        public static void Watch_ValueFactory()
        {
            var watchString = new Watch<string>(() => "foo");
            Assert.False(watchString.IsValueCreated);
            Assert.Equal("foo", watchString.Value);
            Assert.True(watchString.IsValueCreated);
        }

        [Fact]
        public static void Watch_ToString_DoesntForceAllocation()
        {
            var watch = new Watch<object>(() => 1);
            Assert.NotEqual("1", watch.ToString());
            Assert.False(watch.IsValueCreated);

            var value = watch.Value;
            Assert.Equal("1", watch.ToString());
        }

        [Fact]
        public static void Watch_Value_ExceptionRecovery()
        {
            var counter = 0;
            var watch = new Watch<object>(() =>
            {
                if (++counter <= 1)
                {
                    throw new Exception();
                }
                return counter;
            });

            Assert.Throws<Exception>(() => watch.Value);
            Assert.Equal(2, watch.Value);
        }

        [Fact]
        public static void Watch_ValueFactory_Disallow_Reentrance()
        {
            Watch<int> watch = null;
            watch = new Watch<int>(() => watch.Value);

            Assert.Throws<InvalidOperationException>(() => watch.Value);
        }

        [Fact]
        public static void Watch_Value_Watch_Dependency_Change_On_Activity()
        {
            var counter = 0;
            var watch = new Watch<int>(() => GetCounter());

            int GetCounter() => Watcher.Watch(() => ++counter);

            Assert.Equal(1, watch.Value);
            Assert.Equal(1, watch.Value);

            Watcher.StartActivity();

            Assert.Equal(3, watch.Value);
            Assert.Equal(3, watch.Value);

            Watcher.StartActivity();

            Assert.Equal(5, watch.Value);
            Assert.Equal(5, watch.Value);
        }

        [Fact]
        public static void Watch_Value_Nested_Watch()
        {
            var counter = 0;
            var childWatch = new Watch<int>(() => GetCounter());
            var watch = new Watch<int>(() => childWatch.Value);

            int GetCounter() => Watcher.Watch(() => ++counter);

            Assert.Equal(1, watch.Value);
            Assert.Equal(1, watch.Value);

            Watcher.StartActivity();

            Assert.Equal(3, watch.Value);
            Assert.Equal(3, watch.Value);

            Watcher.StartActivity();

            Assert.Equal(5, watch.Value);
            Assert.Equal(5, watch.Value);
        }

        [Fact]
        public static void Watch_Value_Nested_Watcher()
        {
            var counter = 0;
            var watch = new Watch<int>(() => GetCounter());

            int GetCounter() => Watcher.Watch(() => Watcher.Watch(() => ++counter));

            Assert.Equal(1, watch.Value);
            Assert.Equal(1, watch.Value);

            Watcher.StartActivity();

            Assert.Equal(3, watch.Value);
            Assert.Equal(3, watch.Value);

            Watcher.StartActivity();

            Assert.Equal(5, watch.Value);
            Assert.Equal(5, watch.Value);
        }

        [Fact]
        public static void Watch_Value_Watch_Dependency_Change_Token_On_Activity()
        {
            var valueCounter = 0;
            var changeTokenCounter = 0;
            var watch = new Watch<int>(() => GetCounter());

            int GetCounter() => Watcher.Watch(() => ++valueCounter, () => ++changeTokenCounter);

            Assert.Equal(1, watch.Value);
            Assert.Equal(1, watch.Value);

            Watcher.StartActivity();

            Assert.Equal(2, watch.Value);
            Assert.Equal(2, watch.Value);

            Watcher.StartActivity();

            Assert.Equal(3, watch.Value);
            Assert.Equal(3, watch.Value);
        }

        [Fact]
        public static void Watch_Value_Watch_Dependency_In_Parallel()
        {
            var counter = 0;
            var watch = new Watch<int>(() =>
            {
                var n = 0;
                for (var i = 0; i < 100; i++)
                {
                    n += GetCounter();
                }
                return n;
            });

            int GetCounter() => Watcher.Watch(() => ++counter);

            Assert.Equal(5050, watch.Value);
            Assert.Equal(5050, watch.Value);
        }
    }
}
