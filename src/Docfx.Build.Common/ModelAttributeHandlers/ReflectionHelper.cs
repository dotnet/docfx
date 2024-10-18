// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace Docfx.Build.Common;

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
    private static readonly ConcurrentDictionary<Type, List<PropertyInfo>> _gettablePropertiesCache = new();
    private static readonly ConcurrentDictionary<Type, List<PropertyInfo>> _settablePropertiesCache = new();
    private static readonly ConcurrentDictionary<Type, bool> _isDictionaryCache = new();
    private static readonly ConcurrentDictionary<Tuple<Type, Type>, Type> _genericTypeCache = new();
    private static readonly ConcurrentDictionary<PropertyInfo, Func<object, object>> _propertyGetterCache = new();
    private static readonly ConcurrentDictionary<PropertyInfo, Action<object, object>> _propertySetterCache = new();
    private static readonly ConcurrentDictionary<Tuple<Type, Type[], Type[]>, Func<object[], object>> _createInstanceCache = new(StructuralEqualityComparer<Tuple<Type, Type[], Type[]>>.Default);

    public static object CreateInstance(Type type, Type[] typeArguments, Type[] argumentTypes, object[] arguments)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(argumentTypes);
        ArgumentNullException.ThrowIfNull(arguments);

        var func = _createInstanceCache.GetOrAdd(
            Tuple.Create(type, typeArguments ?? [], argumentTypes),
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
            return _ => { throw new ArgumentException("Void is not allowed for type.", nameof(tuple)); };
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
            return _ => { throw new ArgumentException("Void is not allowed for typeArguments.", nameof(tuple)); };
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
            return _ => { throw new ArgumentException("Failed to get ctor of argumentTypes", nameof(tuple)); };
        }
        return GetCreateInstanceFuncCore(ctor, argumentTypes);
    }

    private static Func<object[], object> GetCreateInstanceFuncCore(ConstructorInfo ctor, Type[] argumentTypes)
    {
        var dm = new DynamicMethod(string.Empty, typeof(object), [typeof(object[])]);
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
        ArgumentNullException.ThrowIfNull(type);

        return _settablePropertiesCache.GetOrAdd(type, _getSettableProperties);
    }

    public static List<PropertyInfo> GetGettableProperties(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return _gettablePropertiesCache.GetOrAdd(type, _getGettableProperties);
    }

    public static IEnumerable<PropertyInfo> GetPublicProperties(Type type)
    {
        if (!type.IsVisible)
        {
            return [];
        }
        return GetProperties(type, BindingFlags.Public | BindingFlags.Instance);
    }

    public static bool IsDictionaryType(Type type)
    {
        return _isDictionaryCache.GetOrAdd(type, IsDictionaryTypeCore);
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

        return ImplementsGenericDefinition(type, typeof(IDictionary<,>)) ||
            ImplementsGenericDefinition(type, typeof(IReadOnlyDictionary<,>));
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

    public static bool ImplementsGenericDefinition(Type type, Type genericTypeDefinition)
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
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(prop);

        var func = _propertyGetterCache.GetOrAdd(prop, CreateGetPropertyFunc);
        return func(instance);
    }

    private static Func<object, object> CreateGetPropertyFunc(PropertyInfo prop)
    {
        var dm = new DynamicMethod(string.Empty, typeof(object), [typeof(object)]);
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
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(prop);

        var action = _propertySetterCache.GetOrAdd(prop, CreateSetPropertyFunc);
        action(instance, value);
    }

    private static Action<object, object> CreateSetPropertyFunc(PropertyInfo prop)
    {
        var dm = new DynamicMethod(string.Empty, typeof(void), [typeof(object), typeof(object)]);
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
        public static readonly StructuralEqualityComparer<T> Default = new();

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
