// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerialization.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    internal static class ReflectionExtensions
    {
        /// <summary>
        /// Determines whether the specified type has a default constructor.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///     <c>true</c> if the type has a default constructor; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasDefaultConstructor(this Type type)
        {
            return type.IsValueType || type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) != null;
        }

        public static IEnumerable<PropertyInfo> GetPublicProperties(this Type type)
        {
            var instancePublic = BindingFlags.Instance | BindingFlags.Public;
            if (type.IsInterface)
            {
                return from t in new[] { type }.Concat(type.GetInterfaces())
                       from p in t.GetProperties(instancePublic)
                       select p;
            }
            return type.GetProperties(instancePublic);
        }
    }
}
