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
#if NetCore
                if (interfacetype.IsConstructedGenericType && interfacetype.GetGenericTypeDefinition() == genericInterfaceType)
#else
                if (interfacetype.IsGenericType && interfacetype.GetGenericTypeDefinition() == genericInterfaceType)
#endif
                {
                    return interfacetype;
                }
            }
            return null;
        }

        public static IEnumerable<Type> GetImplementedInterfaces(Type type)
        {
#if NetCore
            var ti = type.GetTypeInfo();
            if (ti.IsInterface)
            {
                yield return type;
            }
#else
            if (type.IsInterface)
#endif
            {
                yield return type;
            }

#if NetCore
            foreach (var implementedInterface in ti.ImplementedInterfaces)
#else
            foreach (var implementedInterface in type.GetInterfaces())
#endif
            {
                yield return implementedInterface;
            }
        }
    }
}
