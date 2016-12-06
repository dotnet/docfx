// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.EntityMergers
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json.Linq;

    public class JObjectMerger : MergerDecorator
    {
        public JObjectMerger(IMerger inner)
            : base(inner)
        {
        }

        public override void Merge(ref object source, object overrides, Type type, IMergeContext context)
        {
            var sourceJObj = source as JObject;
            if (sourceJObj != null && type == typeof(object))
            {
                var overridesDict = overrides as Dictionary<object, object>;
                if (overridesDict != null)
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
                var overrideKey = or.Key as string;
                if (overrideKey != null)
                {
                    object obj;
                    JToken jToken;
                    if (source.TryGetValue(overrideKey, out jToken))
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
}
