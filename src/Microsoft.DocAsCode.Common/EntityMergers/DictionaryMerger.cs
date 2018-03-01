// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.EntityMergers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class DictionaryMerger : MergerDecorator
    {
        public DictionaryMerger(IMerger inner)
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
                        it.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                    {
                        if (overrides == null)
                        {
                            return;
                        }
                        if (it.IsAssignableFrom(overrides.GetType()))
                        {
                            var mergerImplType = typeof(DictionaryMergerImpl<,>).MakeGenericType(it.GetGenericArguments());
                            var instance = (IDictionaryMergerImpl)Activator.CreateInstance(mergerImplType);
                            instance.Merge(source, overrides, context);
                            return;
                        }
                    }
                }
            }

            base.Merge(ref source, overrides, type, context);
        }

        private interface IDictionaryMergerImpl
        {
            void Merge(object source, object overrides, IMergeContext context);
        }

        private sealed class DictionaryMergerImpl<TKey, TValue> : IDictionaryMergerImpl
        {
            public void Merge(object source, object overrides, IMergeContext context)
            {
                Merge((IDictionary<TKey, TValue>)source, (IDictionary<TKey, TValue>)overrides, context);
            }

            public void Merge(IDictionary<TKey, TValue> source, IDictionary<TKey, TValue> overrides, IMergeContext context)
            {
                foreach (var oi in overrides)
                {
                    if (source.TryGetValue(oi.Key, out TValue value))
                    {
                        object refObj = value;
                        context.Merger.Merge(ref refObj, oi.Value, typeof(TValue), context);
                        value = (TValue)refObj;
                    }
                    else
                    {
                        value = oi.Value;
                    }
                    source[oi.Key] = value;
                }
            }
        }
    }
}
