// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common;

public static class ResourcePool
{
    public static ResourcePoolManager<T> Create<T>(Func<T> creator, int maxResourceCount)
        where T : class
    {
        return new ResourcePoolManager<T>(creator, maxResourceCount);
    }
}
