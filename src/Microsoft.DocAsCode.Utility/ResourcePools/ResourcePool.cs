// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Threading.Tasks;

    public static class ResourcePool
    {
        public static ResourcePoolManager<T> Create<T>(Func<T> creator, int maxResourceCount)
            where T : class
        {
            return new ResourcePoolManager<T>(creator, maxResourceCount);
        }

        public static ResourcePoolAsyncManager<T> Create<T>(Func<Task<T>> creator, int maxResourceCount)
            where T : class
        {
            return new ResourcePoolAsyncManager<T>(creator, maxResourceCount);
        }
    }
}
