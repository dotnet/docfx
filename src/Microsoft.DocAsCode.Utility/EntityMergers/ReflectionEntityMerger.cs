// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Utility.EntityMergers
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public class ReflectionEntityMerger
    {
        private readonly ConcurrentDictionary<Type, PropertyMergerImpl> _cache =
            new ConcurrentDictionary<Type, PropertyMergerImpl>();

        public void Merge<T>(ref T source, T overrides) where T : class
        {
            object s = source;
            Merge(ref s, overrides, typeof(T), new MergeContext(this));
            source = (T)s;
        }

        private void Merge(ref object source, object overrides, Type type, MergeContext context)
        {
            if (source == null)
            {
                source = overrides;
                return;
            }
            if (type == typeof(string))
            {
                source = overrides;
                return;
            }
            if (source is IEnumerable)
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
            if (type.IsValueType)
            {
                source = overrides;
                return;
            }
            _cache.GetOrAdd(type, key => new PropertyMergerImpl(key)).Merge(ref source, overrides, context);
        }

        private bool TestKey(object source, object overrides, Type type, MergeContext context)
        {
            if (object.Equals(source, overrides))
            {
                return true;
            }
            return _cache.GetOrAdd(type, key => new PropertyMergerImpl(key)).TestKey(source, overrides, context);
        }

        private sealed class MergeContext
        {
            public MergeContext(ReflectionEntityMerger rootMerger)
            {
                RootMerger = rootMerger;
            }

            public ReflectionEntityMerger RootMerger { get; }
        }

        private sealed class PropInfo
        {
            public PropInfo(PropertyInfo prop, MergeOption option)
            {
                Prop = prop;
                Option = option;
            }

            public PropertyInfo Prop { get; set; }

            public MergeOption Option { get; set; }
        }

        private sealed class PropertyMergerImpl
        {
            private readonly PropInfo[] Props;

            public PropertyMergerImpl(Type type)
            {
                Props = (from prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         where prop.GetGetMethod() != null
                         where prop.GetSetMethod() != null
                         where prop.GetIndexParameters().Length == 0
                         let option = prop.GetCustomAttribute<MergeOptionAttribute>()?.Option ?? MergeOption.Merge
                         where option != MergeOption.Ignore
                         select new PropInfo(prop, option)).ToArray();
            }

            public void Merge(ref object source, object overrides, MergeContext context)
            {
                if (overrides == null)
                {
                    return;
                }
                foreach (var prop in Props)
                {
                    switch (prop.Option)
                    {
                        case MergeOption.Merge:
                        case MergeOption.MergeNullOrDefault:
                            {
                                var s = prop.Prop.GetValue(source);
                                var o = prop.Prop.GetValue(overrides);
                                if (prop.Option == MergeOption.Merge)
                                {
                                    if (o == null)
                                    {
                                        continue;
                                    }
                                    if (o.GetType().IsValueType)
                                    {
                                        var defaultValue = Activator.CreateInstance(o.GetType());
                                        if (object.Equals(defaultValue, o))
                                        {
                                            continue;
                                        }
                                    }
                                }
                                var oldS = s;
                                if (o == null)
                                {
                                    s = o;
                                }
                                else
                                {
                                    context.RootMerger.Merge(ref s, o, prop.Prop.PropertyType, context);
                                }
                                if (!object.ReferenceEquals(s, oldS))
                                {
                                    prop.Prop.SetValue(source, s);
                                }
                                continue;
                            }
                        case MergeOption.Replace:
                        case MergeOption.ReplaceNullOrDefault:
                            {
                                var s = prop.Prop.GetValue(source);
                                var o = prop.Prop.GetValue(overrides);
                                if (prop.Option == MergeOption.Replace)
                                {
                                    if (o == null)
                                    {
                                        continue;
                                    }
                                    if (o.GetType().IsValueType)
                                    {
                                        var defaultValue = Activator.CreateInstance(o.GetType());
                                        if (object.Equals(defaultValue, o))
                                        {
                                            continue;
                                        }
                                    }
                                }
                                prop.Prop.SetValue(source, o);
                                continue;
                            }
                        default:
                            continue;
                    }
                }
            }

            public bool TestKey(object source, object overrides, MergeContext context)
            {
                if (overrides == null)
                {
                    return false;
                }
                return Props.Where(p => p.Option == MergeOption.MergeKey).All(p =>
                {
                    var s = p.Prop.GetValue(source);
                    var o = p.Prop.GetValue(overrides);
                    return object.Equals(s, o);
                });
            }

        }

        private sealed class ListMergerImpl
        {
            public Type ElementType { get; }

            public ListMergerImpl(Type elementType)
            {
                ElementType = elementType;
            }

            public void Merge(IEnumerable source, IEnumerable overrides, MergeContext context)
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
                        if (context.RootMerger.TestKey(si, oi, ElementType, context))
                        {
                            object s = si;
                            context.RootMerger.Merge(ref s, oi, ElementType, context);
                        }
                    }
                }
            }
        }
    }
}