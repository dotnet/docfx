// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using Xunit;

    using Microsoft.DocAsCode.Common;

    [Trait("Owner", "vwxyzh")]
    [Collection("docfx STA")]
    public class ComputerResourceTest
    {
        [Fact]
        public void TestComputerResourceWithLimit()
        {
            var mre0 = new ManualResetEventSlim();
            var mre1 = new ManualResetEventSlim();
            var mre2 = new ManualResetEventSlim();
            var mre3 = new ManualResetEventSlim();
            var mre4 = new ManualResetEventSlim();
            var mre5 = new ManualResetEventSlim();
            var mre6 = new ManualResetEventSlim();
            var list = new List<int>();
            Assert.Equal(ComputerResourceCollection.DefaultDiskIOThreshold, ComputerResource.GetAvailableDiskIOResource());
            using (ComputerResource.Require(ComputerResourceType.DiskIO))
            {
                Assert.Equal(ComputerResourceCollection.DefaultDiskIOThreshold - 1, ComputerResource.GetAvailableDiskIOResource());
                lock (list)
                {
                    list.Add(1);
                }
                using (ComputerResource.NewThread())
                {
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        mre0.Wait();
                        using (ComputerResource.Require(ComputerResourceType.DiskIO))
                        {
                            Assert.Equal(ComputerResourceCollection.DefaultDiskIOThreshold - 2, ComputerResource.GetAvailableDiskIOResource());
                            mre1.Set();
                            mre2.Wait();
                            lock (list)
                            {
                                list.Add(2);
                            }
                            mre4.Wait();
                            Assert.Equal(ComputerResourceCollection.DefaultDiskIOThreshold - 2, ComputerResource.GetAvailableDiskIOResource());
                            mre5.Set();
                        }
                    });
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        mre1.Wait();
                        mre2.Set();
                        using (ComputerResource.Require(ComputerResourceType.DiskIO))
                        {
                            lock (list)
                            {
                                list.Add(3);
                            }
                            mre3.Wait();
                        }
                        mre6.Set();
                    });
                }
                mre0.Set();
                mre1.Wait();
                Assert.Equal(ComputerResourceCollection.DefaultDiskIOThreshold - 2, ComputerResource.GetAvailableDiskIOResource());
                mre3.Set();
                Assert.Equal(ComputerResourceCollection.DefaultDiskIOThreshold - 2, ComputerResource.GetAvailableDiskIOResource());
                mre4.Set();
                mre5.Wait();
            }
            mre6.Wait();
            Assert.Equal(new[] { 1, 2, 3 }, list);
            Assert.Equal(ComputerResourceCollection.DefaultDiskIOThreshold, ComputerResource.GetAvailableDiskIOResource());
        }

        [Fact]
        public void TestComputerResourceWithNestedScope()
        {
            Assert.Equal(Environment.ProcessorCount, ComputerResource.GetAvailableCpuResource());
            Assert.Equal(ComputerResourceCollection.DefaultDiskIOThreshold, ComputerResource.GetAvailableDiskIOResource());
            Assert.Equal(ComputerResourceCollection.DefaultNetworkIOThreshold, ComputerResource.GetAvailableNetworkIOResource());

            using (ComputerResource.Require(ComputerResourceType.Cpu))
            {
                Assert.Equal(Environment.ProcessorCount - 1, ComputerResource.GetAvailableCpuResource());
                Assert.Equal(ComputerResourceCollection.DefaultDiskIOThreshold, ComputerResource.GetAvailableDiskIOResource());
                Assert.Equal(ComputerResourceCollection.DefaultNetworkIOThreshold, ComputerResource.GetAvailableNetworkIOResource());

                using (ComputerResource.Require(ComputerResourceType.DiskIO))
                {
                    Assert.Equal(Environment.ProcessorCount, ComputerResource.GetAvailableCpuResource());
                    Assert.Equal(ComputerResourceCollection.DefaultDiskIOThreshold - 1, ComputerResource.GetAvailableDiskIOResource());
                    Assert.Equal(ComputerResourceCollection.DefaultNetworkIOThreshold, ComputerResource.GetAvailableNetworkIOResource());

                    using (ComputerResource.Require(ComputerResourceType.Cpu | ComputerResourceType.DiskIO))
                    {
                        Assert.Equal(Environment.ProcessorCount - 1, ComputerResource.GetAvailableCpuResource());
                        Assert.Equal(ComputerResourceCollection.DefaultDiskIOThreshold - 1, ComputerResource.GetAvailableDiskIOResource());
                        Assert.Equal(ComputerResourceCollection.DefaultNetworkIOThreshold, ComputerResource.GetAvailableNetworkIOResource());

                        using (ComputerResource.Require(ComputerResourceType.Cpu | ComputerResourceType.NetworkIO))
                        {
                            Assert.Equal(Environment.ProcessorCount - 1, ComputerResource.GetAvailableCpuResource());
                            Assert.Equal(ComputerResourceCollection.DefaultDiskIOThreshold, ComputerResource.GetAvailableDiskIOResource());
                            Assert.Equal(ComputerResourceCollection.DefaultNetworkIOThreshold - 1, ComputerResource.GetAvailableNetworkIOResource());
                        }

                        Assert.Equal(Environment.ProcessorCount - 1, ComputerResource.GetAvailableCpuResource());
                        Assert.Equal(ComputerResourceCollection.DefaultDiskIOThreshold - 1, ComputerResource.GetAvailableDiskIOResource());
                        Assert.Equal(ComputerResourceCollection.DefaultNetworkIOThreshold, ComputerResource.GetAvailableNetworkIOResource());
                    }

                    Assert.Equal(Environment.ProcessorCount, ComputerResource.GetAvailableCpuResource());
                    Assert.Equal(ComputerResourceCollection.DefaultDiskIOThreshold - 1, ComputerResource.GetAvailableDiskIOResource());
                    Assert.Equal(ComputerResourceCollection.DefaultNetworkIOThreshold, ComputerResource.GetAvailableNetworkIOResource());
                }

                Assert.Equal(Environment.ProcessorCount - 1, ComputerResource.GetAvailableCpuResource());
                Assert.Equal(ComputerResourceCollection.DefaultDiskIOThreshold, ComputerResource.GetAvailableDiskIOResource());
                Assert.Equal(ComputerResourceCollection.DefaultNetworkIOThreshold, ComputerResource.GetAvailableNetworkIOResource());
            }
        }
    }
}
