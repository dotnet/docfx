// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Threading;

    public class ComputerResourceCollection
    {
        public const int DefaultDiskIOThreshold = 2;
        public const int DefaultNetworkIOThreshold = 16;

        public ComputerResourceCollection(int cpu = 0, int diskIO = DefaultDiskIOThreshold, int networkIO = DefaultNetworkIOThreshold)
        {
            CpuResource = new SemaphoreSlim(cpu <= 0 ? Environment.ProcessorCount : cpu);
            DiskIOResource = new SemaphoreSlim(diskIO <= 0 ? DefaultDiskIOThreshold : diskIO);
            NetworkIOResource = new SemaphoreSlim(networkIO <= 0 ? DefaultNetworkIOThreshold : networkIO);
        }

        public SemaphoreSlim CpuResource { get; }

        public SemaphoreSlim DiskIOResource { get; }

        public SemaphoreSlim NetworkIOResource { get; }

        public void RequireResource(ComputerResourceType current, ComputerResourceType target)
        {
            if (target == current)
            {
                return;
            }
            switch ((target & ComputerResourceType.Cpu) - (current & ComputerResourceType.Cpu))
            {
                case (int)ComputerResourceType.Cpu:
                    CpuResource.Wait();
                    break;
                case -(int)ComputerResourceType.Cpu:
                    CpuResource.Release();
                    break;
            }
            switch ((target & ComputerResourceType.DiskIO) - (current & ComputerResourceType.DiskIO))
            {
                case (int)ComputerResourceType.DiskIO:
                    DiskIOResource.Wait();
                    break;
                case -(int)ComputerResourceType.DiskIO:
                    DiskIOResource.Release();
                    break;
            }
            switch ((target & ComputerResourceType.NetworkIO) - (current & ComputerResourceType.NetworkIO))
            {
                case (int)ComputerResourceType.NetworkIO:
                    NetworkIOResource.Wait();
                    break;
                case -(int)ComputerResourceType.NetworkIO:
                    NetworkIOResource.Release();
                    break;
            }
        }
    }
}
