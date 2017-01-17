﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public static class ReflectionHelper
    {
        private static readonly ConcurrentDictionary<Type, List<PropertyInfo>> _settablePropertiesCache = new ConcurrentDictionary<Type, List<PropertyInfo>>();
        private static readonly ConcurrentDictionary<Type, List<PropertyInfo>> _gettablePropertiesCache = new ConcurrentDictionary<Type, List<PropertyInfo>>();
        private static readonly ConcurrentDictionary<Type, bool> _isDictionaryCache = new ConcurrentDictionary<Type, bool>();
        private static readonly ConcurrentDictionary<Tuple<Type, Type>, Type> _genericTypeCache = new ConcurrentDictionary<Tuple<Type, Type>, Type>();

        public static List<PropertyInfo> GetSettableProperties(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return _settablePropertiesCache.GetOrAdd(
                type,
                (from prop in GetPublicProperties(type)
                 where prop.GetGetMethod() != null
                 where prop.GetSetMethod() != null
                 where prop.GetIndexParameters().Length == 0
                 select prop).ToList());
        }

        public static List<PropertyInfo> GetGettableProperties(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return _settablePropertiesCache.GetOrAdd(
                type,
                (from prop in GetPublicProperties(type)
                 where prop.GetGetMethod() != null
                 where prop.GetIndexParameters().Length == 0
                 select prop).ToList());
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
    }
}
