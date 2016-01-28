// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerialization.TypeInspectors
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    using YamlDotNet.Core;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.YamlSerialization.Helpers;
    using Microsoft.DocAsCode.YamlSerialization.ObjectDescriptors;

    public class EmitTypeInspector : ExtensibleTypeInspectorSkeleton
    {
        private static ConcurrentDictionary<Type, CachingItem> Cache { get; } =
            new ConcurrentDictionary<Type, CachingItem>();
        private readonly ITypeResolver _resolver;

        public EmitTypeInspector(ITypeResolver resolver)
        {
            _resolver = resolver;
        }

        protected override IEnumerable<IPropertyDescriptor> GetPropertiesCore(Type type, object container)
        {
            CachingItem ci = Cache.GetOrAdd(type, t => CachingItem.Create(t, _resolver));
            if (ci.Error != null)
            {
                throw ci.Error;
            }
            return ci.Properies;
        }

        public override IPropertyDescriptor GetProperty(Type type, object container, string name)
        {
            CachingItem ci = Cache.GetOrAdd(type, t => CachingItem.Create(t, _resolver));
            if (ci.Error != null)
            {
                throw ci.Error;
            }
            if (ci.ExtensibleProperies.Count == 0)
            {
                return null;
            }
            return (from ep in ci.ExtensibleProperies
                    where name.StartsWith(ep.Prefix)
                    select ep.SetName(name)).FirstOrDefault();
        }

        public override IEnumerable<string> GetKeys(Type type, object container)
        {
            CachingItem ci = Cache.GetOrAdd(type, t => CachingItem.Create(t, _resolver));
            if (ci.Error != null)
            {
                throw ci.Error;
            }
            if (ci.ExtensibleProperies.Count == 0)
            {
                return null;
            }
            return from ep in ci.ExtensibleProperies
                   from key in ep.GetAllKeys(container) ?? Enumerable.Empty<string>()
                   select ep.Prefix + key;
        }

        private sealed class CachingItem
        {
            private CachingItem() { }

            public Exception Error { get; private set; }

            public List<EmitPropertyDescriptor> Properies { get; } = new List<EmitPropertyDescriptor>();

            public List<ExtensiblePropertyDescriptor> ExtensibleProperies { get; } = new List<ExtensiblePropertyDescriptor>();

            public static CachingItem Create(Type type, ITypeResolver typeResolver)
            {
                var result = new CachingItem();

                if (!type.IsVisible)
                {
                    result.Error = new YamlException($"Type {type.FullName} is invisible.");
                    return result;
                }
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
                    var propertyType = prop.PropertyType;
                    var extAttr = prop.GetCustomAttribute<ExtensibleMemberAttribute>();
                    if (extAttr == null)
                    {
                        var setMethod = prop.GetSetMethod();
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
                    else
                    {
                        Type valueType = GetGenericValueType(propertyType);

                        if (valueType == null)
                        {
                            result.Error = new YamlException($"Extensible property {prop.Name} in type {type.FullName} do NOT implement IDictionary<string, ?>");
                            return result;
                        }

                        result.ExtensibleProperies.Add(
                            new ExtensiblePropertyDescriptor
                            {
                                KeyReader = CreateDictionaryKeyReader(getMethod, valueType),
                                Prefix = extAttr.Prefix,
                                Reader = CreateDictionaryReader(getMethod, valueType),
                                Writer = CreateDictionaryWriter(getMethod, valueType),
                                Type = valueType,
                                TypeResolver = typeResolver,
                            });
                    }
                }

                // order by the length of Prefix descending.
                result.ExtensibleProperies.Sort((left, right) => right.Prefix.Length - left.Prefix.Length);
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

            private static Type GetGenericValueType(Type propertyType)
            {
                Type valueType = null;
                if (propertyType.IsInterface)
                {
                    valueType = GetGenericValueTypeCore(propertyType);
                }
                valueType = valueType ??
                    (from t in propertyType.GetInterfaces()
                     where t.IsVisible
                     select GetGenericValueTypeCore(t)).FirstOrDefault(x => x != null);
                return valueType;
            }

            private static Type GetGenericValueTypeCore(Type type)
            {
                if (type.IsGenericType &&
                    type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    var args = type.GetGenericArguments();
                    if (args[0] == typeof(string))
                    {
                        return args[1];
                    }
                }
                return null;
            }

            private static Func<object, ICollection<string>> CreateDictionaryKeyReader(MethodInfo getMethod, Type valueType)
            {
                var hostType = getMethod.DeclaringType;
                var propertyType = getMethod.ReturnType;
                var dictType = typeof(IDictionary<,>).MakeGenericType(typeof(string), valueType);
                var dm = new DynamicMethod(string.Empty, typeof(ICollection<string>), new[] { typeof(object) });
                // var dict = (IDictionary<string, T>)((HostType)arg0).Property;
                // return dict?.Keys;
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
                    il.Emit(OpCodes.Castclass, dictType);
                }
                else
                {
                    var notNullLabel = il.DefineLabel();
                    il.DeclareLocal(dictType);
                    il.Emit(OpCodes.Castclass, dictType);
                    il.Emit(OpCodes.Stloc_0);
                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Brtrue_S, notNullLabel);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Ret);
                    il.MarkLabel(notNullLabel);
                    il.Emit(OpCodes.Ldloc_0);
                }
                il.Emit(OpCodes.Callvirt, dictType.GetMethod("get_Keys"));
                il.Emit(OpCodes.Ret);

                return (Func<object, ICollection<string>>)dm.CreateDelegate(typeof(Func<object, ICollection<string>>));
            }

            private static Func<object, string, object> CreateDictionaryReader(MethodInfo getMethod, Type valueType)
            {
                var hostType = getMethod.DeclaringType;
                var propertyType = getMethod.ReturnType;
                var dictType = typeof(IDictionary<,>).MakeGenericType(typeof(string), valueType);
                var dm = new DynamicMethod(string.Empty, typeof(object), new[] { typeof(object), typeof(string) });
                // var dict = (IDictionary<string, T>)((HostType)arg0).Property;
                // if (dict == null) { return null; }
                // T result;
                // if (dict.TryGetValue(arg1, out result)) return result;
                // else return null;
                var il = dm.GetILGenerator();
                il.DeclareLocal(valueType);
                var nullLabel = il.DefineLabel();
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
                    il.Emit(OpCodes.Castclass, dictType);
                }
                else
                {
                    il.DeclareLocal(dictType);
                    il.Emit(OpCodes.Castclass, dictType);
                    il.Emit(OpCodes.Stloc_1);
                    il.Emit(OpCodes.Ldloc_1);
                    il.Emit(OpCodes.Brfalse_S, nullLabel);
                    il.Emit(OpCodes.Ldloc_1);
                }
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldloca_S, (byte)0);
                il.Emit(OpCodes.Callvirt, dictType.GetMethod("TryGetValue"));
                il.Emit(OpCodes.Brfalse_S, nullLabel);
                il.Emit(OpCodes.Ldloc_0);
                if (valueType.IsValueType)
                {
                    il.Emit(OpCodes.Box, valueType);
                }
                il.Emit(OpCodes.Ret);
                il.MarkLabel(nullLabel);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ret);

                return (Func<object, string, object>)dm.CreateDelegate(typeof(Func<object, string, object>));
            }

            private static Action<object, string, object> CreateDictionaryWriter(MethodInfo getMethod, Type valueType)
            {
                var hostType = getMethod.DeclaringType;
                var propertyType = getMethod.ReturnType;
                var dictType = typeof(IDictionary<,>).MakeGenericType(typeof(string), valueType);
                var dm = new DynamicMethod(string.Empty, typeof(void), new[] { typeof(object), typeof(string), typeof(object) });
                // var dict = (IDictionary<string, T>)((HostType)arg0).Property;
                // if (dict != null) { dict[arg1] = (T)arg2; }
                var il = dm.GetILGenerator();
                var nullLabel = il.DefineLabel();
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
                    il.Emit(OpCodes.Castclass, dictType);
                }
                else
                {
                    il.DeclareLocal(dictType);
                    il.Emit(OpCodes.Castclass, dictType);
                    il.Emit(OpCodes.Stloc_0);
                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Brfalse_S, nullLabel);
                    il.Emit(OpCodes.Ldloc_0);
                }
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Unbox_Any, valueType);
                il.Emit(OpCodes.Callvirt, dictType.GetMethod("set_Item"));
                il.MarkLabel(nullLabel);
                il.Emit(OpCodes.Ret);

                return (Action<object, string, object>)dm.CreateDelegate(typeof(Action<object, string, object>));
            }
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

        private sealed class ExtensiblePropertyDescriptor : IPropertyDescriptor
        {
            internal string Prefix { get; set; }

            internal ITypeResolver TypeResolver { get; set; }

            internal Func<object, string, object> Reader { get; set; }

            internal Action<object, string, object> Writer { get; set; }

            internal Func<object, ICollection<string>> KeyReader { get; set; }

            public bool CanWrite => Name != null;

            public string Name { get; private set; }

            public int Order { get; set; }

            public ScalarStyle ScalarStyle { get; set; }

            public Type Type { get; set; }

            public Type TypeOverride { get; set; }

            public T GetCustomAttribute<T>() where T : Attribute
            {
                return null;
            }

            public IObjectDescriptor Read(object target)
            {
                var value = Reader(target, Name.Substring(Prefix.Length));
                return new BetterObjectDescriptor(value, TypeOverride ?? TypeResolver.Resolve(Type, value), Type, ScalarStyle);
            }

            public void Write(object target, object value)
            {
                Writer(target, Name.Substring(Prefix.Length), value);
            }

            public ICollection<string> GetAllKeys(object target)
            {
                if (target == null)
                {
                    return null;
                }
                return KeyReader(target);
            }

            public ExtensiblePropertyDescriptor Clone()
            {
                return (ExtensiblePropertyDescriptor)MemberwiseClone();
            }

            public ExtensiblePropertyDescriptor SetName(string name)
            {
                var result = Clone();
                result.Name = name;
                return result;
            }
        }
    }
}
