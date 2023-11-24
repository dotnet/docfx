// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json.Linq;

namespace Docfx.Common.EntityMergers;

public class JObjectMerger : MergerDecorator
{
    public JObjectMerger(IMerger inner)
        : base(inner)
    {
    }

    public override void Merge(ref object source, object overrides, Type type, IMergeContext context)
    {
        if (source is JObject sourceJObj && type == typeof(object))
        {
            if (overrides is Dictionary<object, object> overridesDict)
            {
                Merge(sourceJObj, overridesDict, context);

                // Stop merging since already merged by context merger
                return;
            }
        }

        base.Merge(ref source, overrides, type, context);
    }

    private static void Merge(JObject source, Dictionary<object, object> overridesDict, IMergeContext context)
    {
        foreach (var or in overridesDict)
        {
            if (or.Key is string overrideKey)
            {
                object obj;
                if (source.TryGetValue(overrideKey, out JToken jToken))
                {
                    obj = jToken;
                    context.Merger.Merge(ref obj, or.Value, typeof(object), context);
                }
                else
                {
                    obj = or.Value;
                }
                source[overrideKey] = JToken.FromObject(obj);
            }
        }
    }
}
