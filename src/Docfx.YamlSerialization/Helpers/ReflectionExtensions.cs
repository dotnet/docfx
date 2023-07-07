// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Docfx.YamlSerialization.Helpers;

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
