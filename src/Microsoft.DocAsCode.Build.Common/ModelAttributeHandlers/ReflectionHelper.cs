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

    public static class ReflectionHelper
    {
        private static ConcurrentDictionary<Type, List<PropertyInfo>> _settablePropertiesCache = new ConcurrentDictionary<Type, List<PropertyInfo>>();
        private static ConcurrentDictionary<Type, List<PropertyInfo>> _gettablePropertiesCache = new ConcurrentDictionary<Type, List<PropertyInfo>>();

        public static List<PropertyInfo> GetSettableProperties(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return _settablePropertiesCache.GetOrAdd(type, (from prop in GetPublicProperties(type)
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

            return _settablePropertiesCache.GetOrAdd(type, (from prop in GetPublicProperties(type)
                                                            where prop.GetGetMethod() != null
                                                            where prop.GetIndexParameters().Length == 0
                                                            select prop).ToList());
        }

        public static IEnumerable<PropertyInfo> GetPublicProperties(Type type)
        {
            return GetProperties(type, BindingFlags.Public | BindingFlags.Instance);
        }

        public static bool IsDictionaryType(Type t)
        {
            if (typeof(IDictionary).IsAssignableFrom(t))
            {
                return true;
            }

            return ImplementsGenericDefintion(t, typeof(IDictionary<,>))
            || ImplementsGenericDefintion(t, typeof(IReadOnlyDictionary<,>));
        }

        public static bool IsIEnumerableType(Type t)
        {
            return typeof(IEnumerable).IsAssignableFrom(t);
        }

        public static bool TryGetGenericType(Type type, Type genericTypeDefinition, out Type genericType)
        {
            genericType = null;
            if (IsGenericType(type, genericTypeDefinition))
            {
                genericType = type;
                return true;
            }
            foreach (var i in type.GetInterfaces())
            {
                if (IsGenericType(i, genericTypeDefinition))
                {
                    genericType = i;
                    return true;
                }
            }

            return false;
        }

        public static bool ImplementsGenericDefintion(Type type, Type genericTypeDefinition)
        {
            if (IsGenericType(type, genericTypeDefinition))
            {
                return true;
            }

            foreach (var i in type.GetInterfaces())
            {
                if (IsGenericType(i, genericTypeDefinition))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsGenericType(Type type, Type genericType)
        {
            return type.IsGenericType
                && type.GetGenericTypeDefinition() == genericType;
        }

        private static IEnumerable<PropertyInfo> GetProperties(Type type, BindingFlags bindingFlags)
        {
            List<PropertyInfo> props = new List<PropertyInfo>(type.GetProperties(bindingFlags));
            if (type.IsInterface)
            {
                foreach (var i in type.GetInterfaces())
                {
                    props.AddRange(i.GetProperties(bindingFlags));
                }
            }

            return props;
        }
    }
}
