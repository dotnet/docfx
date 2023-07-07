// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Exceptions;

using Newtonsoft.Json.Linq;

namespace Docfx.Common.EntityMergers;

public class JArrayMerger : MergerDecorator
{
    public JArrayMerger(IMerger inner)
        : base(inner)
    {
    }

    public override void Merge(ref object source, object overrides, Type type, IMergeContext context)
    {
        if (source is JArray sourceJArray && type == typeof(object))
        {
            if (overrides is List<object> overridesList)
            {
                Merge(sourceJArray, overridesList, context);

                // Stop merging since already merged by context merger
                return;
            }
        }

        base.Merge(ref source, overrides, type, context);
    }

    private static void Merge(JArray source, List<object> overridesList, IMergeContext context)
    {
        if (source.Count != overridesList.Count)
        {
            // We have assumption that JArray can be merged only if the count is the same
            throw new DocfxException($"The count '{source.Count}' of JArray is different from overwrite list {overridesList.Count} ");
        }
        for (var i = 0; i < source.Count; i++)
        {
            object sourceItem = source[i];
            var overwriteItem = overridesList[i];
            if (overwriteItem == null)
            {
                continue;
            }

            context.Merger.Merge(ref sourceItem, overwriteItem, typeof(object), context);
            source[i] = JToken.FromObject(sourceItem);
        }
    }
}
