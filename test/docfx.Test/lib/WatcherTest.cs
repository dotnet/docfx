// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
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

            int GetCounter() => Watcher.Read(() => ++counter);

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

            int GetCounter() => Watcher.Read(() => ++counter);

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

            int GetCounter() => Watcher.Read(() => Watcher.Read(() => ++counter));

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

            int GetCounter() => Watcher.Read(() => ++valueCounter, () => ++changeTokenCounter);

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
        public static void Watch_Value_Reevaluate_On_Any_Dependency_Change()
        {
            var counter = 0;
            var watch = new Watch<int>(() => GetCounterChange() + GetCounterNoChange());

            int GetCounterNoChange() => Watcher.Read(() => 0);
            int GetCounterChange() => Watcher.Read(() => ++counter);

            Assert.Equal(1, watch.Value);
            Assert.Equal(1, watch.Value);

            Watcher.StartActivity();

            Assert.Equal(3, watch.Value);
            Assert.Equal(3, watch.Value);
        }

        [Fact]
        public static void Watch_Value_Do_Not_Reevaluate_On_No_Dependency_Change()
        {
            var counter = 0;
            var watch = new Watch<int>(() => ++counter + GetCounterNoChange());

            int GetCounterNoChange() => Watcher.Read(() => 0);

            Assert.Equal(1, watch.Value);
            Assert.Equal(1, watch.Value);

            Watcher.StartActivity();

            Assert.Equal(1, watch.Value);
            Assert.Equal(1, watch.Value);
        }

        [Fact]
        public static void Watch_Created_Value()
        {
            var counter = 0;
            var childCounter = 0;
            var parentCounter = 0;
            var child = new Watch<int>(() => ++childCounter + Watcher.Read(() => 0, () => ++counter));
            var parent = new Watch<int>(() => ++parentCounter + child.Value);

            Assert.Equal(1, child.Value);
            Assert.True(child.IsValueCreated);
            Assert.Equal(2, parent.Value);

            Watcher.StartActivity();

            Assert.Equal(4, parent.Value);
        }

        [Fact]
        public static void Watch_Value_Rebuild_Dependency_Graph_On_Dependency_Change()
        {
            var exists = false;
            var counter = 100;
            var watch = new Watch<int>(() =>
            {
                if (FileExists())
                {
                    return ReadFile();
                }
                return 0;
            });

            bool FileExists() => Watcher.Read(() => exists);
            int ReadFile() => Watcher.Read(() => counter);

            Assert.Equal(0, watch.Value);
            Assert.Equal(0, watch.Value);

            Watcher.StartActivity();
            exists = true;

            Assert.Equal(100, watch.Value);
            Assert.Equal(100, watch.Value);

            Watcher.StartActivity();
            counter++;

            Assert.Equal(101, watch.Value);
            Assert.Equal(101, watch.Value);
        }

        [Fact]
        public static void Watch_Value_Watch_Dependency_In_Parallel()
        {
            var counter = 0;
            var watch = new Watch<int>(() =>
            {
                var n = 0;
                Parallel.For(0, 100, i =>
                {
                    Interlocked.Add(ref n, GetCounter());
                });
                return n;
            });

            int GetCounter() => Watcher.Read(() => Interlocked.Increment(ref counter));

            Assert.Equal(5050, watch.Value);
            Assert.Equal(5050, watch.Value);
        }
    }
}
