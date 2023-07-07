// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common.EntityMergers;

public class MergerFacade
{
    private readonly IMerger _merger;

    public MergerFacade(IMerger merger)
    {
        _merger = merger;
    }

    public void Merge<T>(ref T source, T overrides, IReadOnlyDictionary<string, object> data = null) where T : class
    {
        object s = source;
        var context = new MergeContext(_merger, data);
        context.Merger.Merge(ref s, overrides, typeof(T), context);
        source = (T)s;
    }
}
