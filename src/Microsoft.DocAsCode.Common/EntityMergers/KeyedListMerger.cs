// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.EntityMergers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class KeyedListMerger : MergerDecorator
    {
        public KeyedListMerger(IMerger inner)
            : base(inner)
        {
        }

        public override void Merge(ref object source, object overrides, Type type, IMergeContext context)
        {
            if (source is IEnumerable && type != typeof(string))
            {
                foreach (var it in type.GetInterfaces())
                {
                    if (it.IsGenericType &&
                        it.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        if (overrides == null)
                        {
                            return;
                        }
                        if (it.IsAssignableFrom(overrides.GetType()))
                        {
                            new ListMergerImpl(it.GetGenericArguments()[0]).Merge((IEnumerable)source, (IEnumerable)overrides, context);
                            return;
                        }
                    }
                }
            }

            base.Merge(ref source, overrides, type, context);
        }


        private sealed class ListMergerImpl
        {
            public Type ElementType { get; }

            public ListMergerImpl(Type elementType)
            {
                ElementType = elementType;
            }

            public void Merge(IEnumerable source, IEnumerable overrides, IMergeContext context)
            {
                foreach (var oi in overrides)
                {
                    if (oi == null)
                    {
                        continue;
                    }
                    foreach (var si in source)
                    {
                        if (si == null)
                        {
                            continue;
                        }
                        if (context.Merger.TestKey(si, oi, ElementType, context))
                        {
                            object s = si;
                            context.Merger.Merge(ref s, oi, ElementType, context);
                        }
                    }
                }
            }
        }
    }
}
