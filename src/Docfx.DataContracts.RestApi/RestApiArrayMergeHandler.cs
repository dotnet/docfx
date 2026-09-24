// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using Docfx.Common.EntityMergers;
using Docfx.Exceptions;

namespace Docfx.DataContracts.RestApi;

// REST arrays have positional overwrite semantics, including null placeholders.
// They must not use the entity merger's default key-based list matching.
public sealed class RestApiArrayMergeHandler : IMergeHandler
{
    public void Merge(ref object source, object overrides, IMergeContext context)
    {
        if (source == null)
        {
            source = overrides;
            return;
        }
        var items = (IList)source;
        var replacements = (IList)overrides;
        if (items.Count != replacements.Count)
        {
            throw new DocfxException($"The count '{items.Count}' of REST array is different from overwrite list {replacements.Count}");
        }
        var itemType = source.GetType().GetGenericArguments()[0];
        for (var i = 0; i < items.Count; i++)
        {
            if (replacements[i] == null) continue;
            var item = items[i];
            context.Merger.Merge(ref item, replacements[i], itemType, context);
            items[i] = item;
        }
    }
}
