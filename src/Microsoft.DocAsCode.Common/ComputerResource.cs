// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Runtime.Remoting.Messaging;
    using System.Threading;

    [Serializable]
    public sealed class ComputerResource : IDisposable
    {
        public const int DefaultDiskIOThreshold = 2;
        public const int DefaultNetworkIOThreshold = 16;

        private static readonly SemaphoreSlim CpuResource = new SemaphoreSlim(Environment.ProcessorCount);
        private static readonly SemaphoreSlim DiskIOResource = new SemaphoreSlim(DefaultDiskIOThreshold);
        private static readonly SemaphoreSlim NetworkIOResource = new SemaphoreSlim(DefaultNetworkIOThreshold);

        private readonly ComputerResource _outer;
        private readonly ComputerResourceType _type;

        private ComputerResource(ComputerResourceType type)
        {
            _type = type;
            _outer = GetResourceType();
            SetResourceType(this);
            RequireResource(_outer?._type ?? ComputerResourceType.None, type);
        }

        public static ComputerResource Require(ComputerResourceType type)
        {
            return new ComputerResource(type);
        }

        public static ComputerResource NewThread()
        {
            return new ComputerResource(ComputerResourceType.None);
        }

        private static void SetResourceType(ComputerResource resource)
        {
            if (resource == null)
            {
                CallContext.FreeNamedDataSlot(nameof(ComputerResource));
            }
            else
            {
                CallContext.LogicalSetData(nameof(ComputerResource), resource);
            }
        }

        private static ComputerResource GetResourceType()
        {
            return (ComputerResource)CallContext.LogicalGetData(nameof(ComputerResource));
        }

        private static void RequireResource(ComputerResourceType current, ComputerResourceType target)
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
                default:
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
                default:
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
                default:
                    break;
            }
        }

        public static int GetAvailableCpuResource() => CpuResource.CurrentCount;

        public static int GetAvailableDiskIOResource() => DiskIOResource.CurrentCount;

        public static int GetAvailableNetworkIOResource() => NetworkIOResource.CurrentCount;

        public void Dispose()
        {
            SetResourceType(_outer);
            RequireResource(_type, _outer?._type ?? ComputerResourceType.None);
        }
    }
}
