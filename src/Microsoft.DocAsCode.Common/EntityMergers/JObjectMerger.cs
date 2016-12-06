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
            var stopMerge = false;
            var sourceJObj = source as JObject;
            if (sourceJObj != null)
            {
                var overridesDict = overrides as Dictionary<object, object>;
                if (overridesDict != null)
                {
                    foreach (var or in overridesDict)
                    {
                        var strKey = or.Key as string;
                        if (strKey != null)
                        {
                            Merge(sourceJObj, strKey, or.Value, context);

                            // Stop merging since already merged by context merger
                            stopMerge = true;
                        }
                    }
                }
            }

            if (!stopMerge)
            {
                base.Merge(ref source, overrides, type, context);
            }
        }

        private static void Merge(JObject source, string overrideKey, object overrideValue, IMergeContext context)
        {
            object obj;
            JToken jToken;
            if (source.TryGetValue(overrideKey, out jToken))
            {
                obj = jToken;
                context.Merger.Merge(ref obj, overrideValue, typeof(object), context);
            }
            else
            {
                obj = overrideValue;
            }
            source[overrideKey] = JToken.FromObject(obj);
        }
    }
}
