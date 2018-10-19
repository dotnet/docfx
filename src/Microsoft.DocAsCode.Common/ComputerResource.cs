// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Threading;

    [Serializable]
    public sealed class ComputerResource : IDisposable
    {
        private static ComputerResourceCollection _resources = new ComputerResourceCollection();

        private readonly ComputerResource _outer;
        private readonly ComputerResourceType _type;

        private ComputerResource(ComputerResourceType type, bool freeSlot)
        {
            _type = type;
            _outer = GetResourceType();
            SetResourceType(freeSlot ? null : this);
            _resources.RequireResource(_outer?._type ?? ComputerResourceType.None, type);
        }

        public static void SetResources(ComputerResourceCollection resources)
        {
            _resources = resources ?? throw new ArgumentNullException(nameof(resources));
        }

        public static ComputerResource Require(ComputerResourceType type)
        {
            return new ComputerResource(type, false);
        }

        public static ComputerResource NewThread()
        {
            return new ComputerResource(ComputerResourceType.None, true);
        }

        private static void SetResourceType(ComputerResource resource)
        {
            if (resource == null)
            {
                LogicalCallContext.FreeData(nameof(ComputerResource));
            }
            else
            {
                LogicalCallContext.SetData(nameof(ComputerResource), resource);
            }
        }

        private static ComputerResource GetResourceType()
        {
            return (ComputerResource)LogicalCallContext.GetData(nameof(ComputerResource));
        }

        public static int GetAvailableCpuResource() => _resources.CpuResource.CurrentCount;

        public static int GetAvailableDiskIOResource() => _resources.DiskIOResource.CurrentCount;

        public static int GetAvailableNetworkIOResource() => _resources.NetworkIOResource.CurrentCount;

        public void Dispose()
        {
            SetResourceType(_outer);
            _resources.RequireResource(_type, _outer?._type ?? ComputerResourceType.None);
        }
    }
}
