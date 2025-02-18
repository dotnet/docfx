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
    /// <param name="allowPrivateConstructors">Allow private constructor.</param>
    /// <returns>
    ///     <c>true</c> if the type has a default constructor; otherwise, <c>false</c>.
    /// </returns>
    public static bool HasDefaultConstructor(this Type type, bool allowPrivateConstructors)
    {
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (allowPrivateConstructors)
        {
            bindingFlags |= BindingFlags.NonPublic;
        }
        return type.IsValueType || type.GetConstructor(bindingFlags, null, Type.EmptyTypes, null) != null;
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
