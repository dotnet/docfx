// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.EntityMergers
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Reflection;

    public class ReflectionEntityMerger : IMerger
    {
        private readonly ConcurrentDictionary<Type, PropertyMergerImpl> _cache =
            new ConcurrentDictionary<Type, PropertyMergerImpl>();

        private void Merge(ref object source, object overrides, Type type, IMergeContext context)
        {
            if (source == null)
            {
                source = overrides;
                return;
            }
            if (type == typeof(string) || type == typeof(object))
            {
                source = overrides;
                return;
            }
            if (type.IsValueType)
            {
                source = overrides;
                return;
            }
            _cache.GetOrAdd(type, key => new PropertyMergerImpl(key)).Merge(ref source, overrides, context);
        }

        void IMerger.Merge(ref object source, object overrides, Type type, IMergeContext context)
        {
            Merge(ref source, overrides, type, context);
        }

        bool IMerger.TestKey(object source, object overrides, Type type, IMergeContext context)
        {
            if (object.Equals(source, overrides))
            {
                return true;
            }
            return _cache.GetOrAdd(type, key => new PropertyMergerImpl(key)).TestKey(source, overrides, context);
        }

        private sealed class PropInfo
        {
            public PropertyInfo Prop { get; set; }

            public MergeOption Option { get; set; }

            public IMergeHandler Handler { get; set; }
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
                         let attr = prop.GetCustomAttribute<MergeOptionAttribute>()
                         where attr?.Option != MergeOption.Ignore
                         select new PropInfo
                         {
                             Prop = prop,
                             Option = attr?.Option ?? MergeOption.Merge,
                             Handler = attr?.Handler,
                         }).ToArray();
            }

            public void Merge(ref object source, object overrides, IMergeContext context)
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
                                else if (prop.Handler != null)
                                {
                                    prop.Handler.Merge(ref s, o, context);
                                }
                                else
                                {
                                    context.Merger.Merge(ref s, o, prop.Prop.PropertyType, context);
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

            public bool TestKey(object source, object overrides, IMergeContext context)
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
    }
}