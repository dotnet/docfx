// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerialization.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

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
}
