// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DocAsCode.Common;

namespace Microsoft.DocAsCode.Build.Engine;

internal class RendererWithResourcePool : ITemplateRenderer
{
    private readonly ResourcePoolManager<ITemplateRenderer> _rendererPool;
    public RendererWithResourcePool(Func<ITemplateRenderer> creater, int maxParallelism)
    {
        _rendererPool = ResourcePool.Create(creater, maxParallelism);

        using var lease = _rendererPool.Rent();
        var inner = lease.Resource;
        Raw = inner.Raw;
        Dependencies = inner.Dependencies;
        Path = inner.Path;
        Name = inner.Name;
    }

    public IEnumerable<string> Dependencies { get; }

    public string Raw { get; }

    public string Path { get; }

    public string Name { get; }

    public string Render(object model)
    {
        if (model == null)
        {
            return null;
        }

        using var lease = _rendererPool.Rent();
        return lease.Resource?.Render(model);
    }
}
