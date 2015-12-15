// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerialization.TypeInspectors
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    using YamlDotNet.Core;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.TypeInspectors;

    using Microsoft.DocAsCode.YamlSerialization.Helpers;
    using Microsoft.DocAsCode.YamlSerialization.ObjectDescriptors;

    public class EmitTypeInspector : TypeInspectorSkeleton
    {
        private static ConcurrentDictionary<Type, CachingItem> Cache { get; } =
            new ConcurrentDictionary<Type, CachingItem>();
        private readonly ITypeResolver _resolver;

        public EmitTypeInspector(ITypeResolver resolver)
        {
            _resolver = resolver;
        }

        public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object container)
        {
            CachingItem ci = Cache.GetOrAdd(type, t => CachingItem.Create(t, _resolver));
            if (ci == null)
            {
                throw new NotSupportedException($"Type {type.FullName} is invisible.");
            }
            return ci.Properies;
        }

        private sealed class CachingItem
        {
            private CachingItem() { }

            public static CachingItem Create(Type type, ITypeResolver typeResolver)
            {
                if (!type.IsVisible)
                {
                    return null;
                }

                var result = new CachingItem();
                foreach (var prop in type.GetPublicProperties())
                {
                    if (prop.GetIndexParameters().Length > 0)
                    {
                        continue;
                    }
                    var getMethod = prop.GetGetMethod();
                    if (getMethod == null)
                    {
                        continue;
                    }
                    var setMethod = prop.GetSetMethod();
                    var propertyType = prop.PropertyType;
                    result.Properies.Add(new EmitPropertyDescriptor
                    {
                        CanWrite = setMethod != null,
                        Name = prop.Name,
                        Property = prop,
                        Type = propertyType,
                        TypeResolver = typeResolver,
                        Reader = CreateReader(getMethod),
                        Writer = setMethod == null ? null : CreateWriter(setMethod),
                    });
                }

                return result;
            }

            private static Func<object, object> CreateReader(MethodInfo getMethod)
            {
                var hostType = getMethod.DeclaringType;
                var propertyType = getMethod.ReturnType;
                var dm = new DynamicMethod(string.Empty, typeof(object), new[] { typeof(object) });
                var il = dm.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                if (hostType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox, hostType);
                    il.Emit(OpCodes.Call, getMethod);
                }
                else
                {
                    il.Emit(OpCodes.Castclass, hostType);
                    il.Emit(OpCodes.Callvirt, getMethod);
                }
                if (propertyType.IsValueType)
                {
                    il.Emit(OpCodes.Box, propertyType);
                }
                il.Emit(OpCodes.Ret);
                return (Func<object, object>)dm.CreateDelegate(typeof(Func<object, object>));
            }

            private static Action<object, object> CreateWriter(MethodInfo setMethod)
            {
                var hostType = setMethod.DeclaringType;
                var propertyType = setMethod.GetParameters()[0].ParameterType;
                var dm = new DynamicMethod(string.Empty, typeof(void), new[] { typeof(object), typeof(object) });
                var il = dm.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                var isValueType = hostType.IsValueType;
                if (isValueType)
                {
                    il.Emit(OpCodes.Unbox, hostType);
                }
                else
                {
                    il.Emit(OpCodes.Castclass, hostType);
                }
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Unbox_Any, propertyType);
                il.Emit(isValueType ? OpCodes.Call : OpCodes.Callvirt, setMethod);
                il.Emit(OpCodes.Ret);
                return (Action<object, object>)dm.CreateDelegate(typeof(Action<object, object>));
            }

            public List<EmitPropertyDescriptor> Properies { get; } = new List<EmitPropertyDescriptor>();
        }

        private sealed class EmitPropertyDescriptor : IPropertyDescriptor
        {
            private readonly Dictionary<Type, Attribute> _attributeCache = new Dictionary<Type, Attribute>();

            internal PropertyInfo Property { get; set; }

            internal ITypeResolver TypeResolver { get; set; }

            internal Func<object, object> Reader { get; set; }

            internal Action<object, object> Writer { get; set; }

            public bool CanWrite { get; set; }

            public string Name { get; set; }

            public int Order { get; set; }

            public ScalarStyle ScalarStyle { get; set; }

            public Type Type { get; set; }

            public Type TypeOverride { get; set; }

            public T GetCustomAttribute<T>() where T : Attribute
            {
                Attribute attribute;
                if (_attributeCache.TryGetValue(typeof(T), out attribute))
                {
                    return (T)attribute;
                }
                var result = Property.GetCustomAttribute<T>();
                _attributeCache[typeof(T)] = result;
                return result;
            }

            public IObjectDescriptor Read(object target)
            {
                var value = Reader(target);
                return new BetterObjectDescriptor(value, TypeOverride ?? TypeResolver.Resolve(Type, value), Type, ScalarStyle);
            }

            public void Write(object target, object value)
            {
                Writer(target, value);
            }
        }
    }
}
