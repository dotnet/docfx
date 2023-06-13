// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DocAsCode.YamlSerialization.Helpers;

internal static class ReflectionUtility
{
    public static Type GetImplementedGenericInterface(Type type, Type genericInterfaceType)
    {
        foreach (var interfacetype in GetImplementedInterfaces(type))
        {
            if (interfacetype.IsGenericType && interfacetype.GetGenericTypeDefinition() == genericInterfaceType)
            {
                return interfacetype;
            }
        }
        return null;
    }

    public static IEnumerable<Type> GetImplementedInterfaces(Type type)
    {
        if (type.IsInterface)
        {
            yield return type;
        }

        foreach (var implementedInterface in type.GetInterfaces())
        {
            yield return implementedInterface;
        }
    }
}
