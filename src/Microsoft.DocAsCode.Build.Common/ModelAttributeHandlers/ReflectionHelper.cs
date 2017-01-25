// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    public static class ReflectionHelper
    {
        private static readonly Func<Type, List<PropertyInfo>> _getGettableProperties =
            t => (from prop in GetPublicProperties(t)
                  where prop.GetGetMethod() != null
                  where prop.GetIndexParameters().Length == 0
                  select prop).ToList();
        private static readonly Func<Type, List<PropertyInfo>> _getSettableProperties =
            t => (from prop in GetPublicProperties(t)
                  where prop.GetGetMethod() != null
                  where prop.GetSetMethod() != null
                  where prop.GetIndexParameters().Length == 0
                  select prop).ToList();
        private static readonly ConcurrentDictionary<Type, List<PropertyInfo>> _gettablePropertiesCache = new ConcurrentDictionary<Type, List<PropertyInfo>>();
        private static readonly ConcurrentDictionary<Type, List<PropertyInfo>> _settablePropertiesCache = new ConcurrentDictionary<Type, List<PropertyInfo>>();
        private static readonly ConcurrentDictionary<Type, bool> _isDictionaryCache = new ConcurrentDictionary<Type, bool>();
        private static readonly ConcurrentDictionary<Tuple<Type, Type>, Type> _genericTypeCache = new ConcurrentDictionary<Tuple<Type, Type>, Type>();
        private static readonly ConcurrentDictionary<PropertyInfo, Func<object, object>> _propertyGetterCache = new ConcurrentDictionary<PropertyInfo, Func<object, object>>();
        private static readonly ConcurrentDictionary<PropertyInfo, Action<object, object>> _propertySetterCache = new ConcurrentDictionary<PropertyInfo, Action<object, object>>();
        private static readonly ConcurrentDictionary<Tuple<Type, Type[], Type[]>, Func<object[], object>> _createInstanceCache = new ConcurrentDictionary<Tuple<Type, Type[], Type[]>, Func<object[], object>>(StructuralEqualityComparer<Tuple<Type, Type[], Type[]>>.Default);

        public static object CreateInstance(Type type, Type[] typeArguments, Type[] argumentTypes, object[] arguments)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (argumentTypes == null)
            {
                throw new ArgumentNullException(nameof(argumentTypes));
            }
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }
            var func = _createInstanceCache.GetOrAdd(
                Tuple.Create(type, typeArguments ?? Type.EmptyTypes, argumentTypes),
                GetCreateInstanceFunc);
            return func(arguments);
        }

        private static Func<object[], object> GetCreateInstanceFunc(Tuple<Type, Type[], Type[]> tuple)
        {
            var type = tuple.Item1;
            var typeArguments = tuple.Item2;
            var argumentTypes = tuple.Item3;
            if (type == typeof(void))
            {
                return _ => { throw new ArgumentException("Void is not allowed.", nameof(type)); };
            }
            if (type.IsValueType)
            {
                var message = $"Value type ({type.FullName}) is not supported.";
                return _ => { throw new NotSupportedException(message); };
            }
            if (!type.IsVisible)
            {
                var message = $"{nameof(type)}({type.FullName}) is invisible.";
                return _ => { throw new NotSupportedException(message); };
            }
            if (Array.IndexOf(typeArguments, typeof(void)) != -1)
            {
                return _ => { throw new ArgumentException("Void is not allowed.", nameof(typeArguments)); };
            }
            var typeArgument = Array.Find(typeArguments, t => !t.IsVisible);
            if (typeArgument != null)
            {
                var message = $"{nameof(typeArguments)}({typeArgument}) is invisible.";
                return _ => { throw new NotSupportedException(message); };
            }
            typeArgument = Array.Find(typeArguments, t => t.IsByRef || t.IsPointer);
            if (typeArgument != null)
            {
                var message = $"{nameof(typeArguments)}({typeArgument}) is not supported.";
                return _ => { throw new NotSupportedException(message); };
            }
            var argumentType = Array.Find(argumentTypes, t => !t.IsVisible);
            if (argumentType != null)
            {
                var message = $"{nameof(argumentTypes)}({argumentType}) is invisible.";
                return _ => { throw new NotSupportedException(message); };
            }
            argumentType = Array.Find(argumentTypes, t => t.IsByRef || t.IsPointer);
            if (argumentType != null)
            {
                var message = $"{nameof(argumentTypes)}({argumentType}) is not supported.";
                return _ => { throw new NotSupportedException(message); };
            }
            Type finalType;
            if (type.IsGenericTypeDefinition)
            {
                try
                {
                    finalType = type.MakeGenericType(typeArguments);
                }
                catch (Exception ex)
                {
                    return _ => { throw ex; };
                }
            }
            else
            {
                if (typeArguments.Length > 0)
                {
                    return _ => { throw new ArgumentException(nameof(typeArguments)); };
                }
                finalType = type;
            }
            var ctor = finalType.GetConstructor(argumentTypes);
            if (ctor == null)
            {
                return _ => { throw new ArgumentException(nameof(argumentTypes)); };
            }
            return GetCreateInstanceFuncCore(ctor, argumentTypes);
        }

        private static Func<object[], object> GetCreateInstanceFuncCore(ConstructorInfo ctor, Type[] argumentTypes)
        {
            var dm = new DynamicMethod(string.Empty, typeof(object), new[] { typeof(object[]) });
            var il = dm.GetILGenerator();
            for (int i = 0; i < argumentTypes.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldelem_Ref);
                il.Emit(OpCodes.Unbox_Any, argumentTypes[i]);
            }
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Ret);
            return (Func<object[], object>)dm.CreateDelegate(typeof(Func<object[], object>));
        }

        public static List<PropertyInfo> GetSettableProperties(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return _settablePropertiesCache.GetOrAdd(type, _getSettableProperties);
        }

        public static List<PropertyInfo> GetGettableProperties(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return _gettablePropertiesCache.GetOrAdd(type, _getGettableProperties);
        }

        public static IEnumerable<PropertyInfo> GetPublicProperties(Type type)
        {
            if (!type.IsVisible)
            {
                return Enumerable.Empty<PropertyInfo>();
            }
            return GetProperties(type, BindingFlags.Public | BindingFlags.Instance);
        }

        public static bool IsDictionaryType(Type type)
        {
            return _isDictionaryCache.GetOrAdd(type, t => IsDictionaryTypeCore(t));
        }

        private static bool IsDictionaryTypeCore(Type type)
        {
            if (!IsIEnumerableType(type))
            {
                return false;
            }
            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                return true;
            }

            return ImplementsGenericDefintion(type, typeof(IDictionary<,>)) ||
                ImplementsGenericDefintion(type, typeof(IReadOnlyDictionary<,>));
        }

        public static bool IsIEnumerableType(Type t)
        {
            return typeof(IEnumerable).IsAssignableFrom(t);
        }

        public static Type GetGenericType(Type type, Type genericTypeDefinition)
        {
            return _genericTypeCache.GetOrAdd(Tuple.Create(type, genericTypeDefinition), t => GetGenericTypeNoCache(t.Item1, t.Item2));
        }

        private static Type GetGenericTypeNoCache(Type type, Type genericTypeDefinition)
        {
            if (type.IsInterface == genericTypeDefinition.IsInterface &&
                IsGenericType(type, genericTypeDefinition))
            {
                return type;
            }
            if (genericTypeDefinition.IsInterface)
            {
                foreach (var i in type.GetInterfaces())
                {
                    if (IsGenericType(i, genericTypeDefinition))
                    {
                        return i;
                    }
                }
            }
            return null;
        }

        public static bool TryGetGenericType(Type type, Type genericTypeDefinition, out Type genericType)
        {
            genericType = GetGenericType(type, genericTypeDefinition);
            return genericType != null;
        }

        public static bool ImplementsGenericDefintion(Type type, Type genericTypeDefinition)
        {
            return GetGenericType(type, genericTypeDefinition) != null;
        }

        public static bool IsGenericType(Type type, Type genericTypeDefinition)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == genericTypeDefinition;
        }

        private static IEnumerable<PropertyInfo> GetProperties(Type type, BindingFlags bindingFlags)
        {
            IEnumerable<PropertyInfo> results = type.GetProperties(bindingFlags);
            if (type.IsInterface)
            {
                foreach (var i in type.GetInterfaces())
                {
                    if (i.IsVisible)
                    {
                        results = results.Concat(i.GetProperties(bindingFlags));
                    }
                }
            }

            return results;
        }

        public static object GetPropertyValue(object instance, PropertyInfo prop)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (prop == null)
            {
                throw new ArgumentNullException(nameof(prop));
            }
            var func = _propertyGetterCache.GetOrAdd(prop, CreateGetPropertyFunc);
            return func(instance);
        }

        private static Func<object, object> CreateGetPropertyFunc(PropertyInfo prop)
        {
            var dm = new DynamicMethod(string.Empty, typeof(object), new[] { typeof(object) });
            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            if (prop.DeclaringType.IsValueType)
            {
                il.Emit(OpCodes.Unbox, prop.DeclaringType);
                il.Emit(OpCodes.Call, prop.GetGetMethod());
            }
            else
            {
                il.Emit(OpCodes.Castclass, prop.DeclaringType);
                il.Emit(OpCodes.Callvirt, prop.GetGetMethod());
            }
            if (prop.PropertyType.IsValueType)
            {
                il.Emit(OpCodes.Box, prop.PropertyType);
            }
            il.Emit(OpCodes.Ret);
            return (Func<object, object>)dm.CreateDelegate(typeof(Func<object, object>));
        }

        public static void SetPropertyValue(object instance, PropertyInfo prop, object value)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (prop == null)
            {
                throw new ArgumentNullException(nameof(prop));
            }
            var action = _propertySetterCache.GetOrAdd(prop, CreateSetPropertyFunc);
            action(instance, value);
        }

        private static Action<object, object> CreateSetPropertyFunc(PropertyInfo prop)
        {
            var dm = new DynamicMethod(string.Empty, typeof(void), new[] { typeof(object), typeof(object) });
            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            if (prop.DeclaringType.IsValueType)
            {
                il.Emit(OpCodes.Unbox, prop.DeclaringType);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Unbox_Any, prop.PropertyType);
                il.Emit(OpCodes.Call, prop.GetSetMethod());
            }
            else
            {
                il.Emit(OpCodes.Castclass, prop.DeclaringType);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Unbox_Any, prop.PropertyType);
                il.Emit(OpCodes.Callvirt, prop.GetSetMethod());
            }
            il.Emit(OpCodes.Ret);
            return (Action<object, object>)dm.CreateDelegate(typeof(Action<object, object>));
        }

        private sealed class StructuralEqualityComparer<T> : IEqualityComparer<T>
        {
            public static readonly StructuralEqualityComparer<T> Default = new StructuralEqualityComparer<T>();

            public bool Equals(T x, T y)
            {
                return StructuralComparisons.StructuralEqualityComparer.Equals(x, y);
            }

            public int GetHashCode(T obj)
            {
                return StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj);
            }
        }
    }
}
