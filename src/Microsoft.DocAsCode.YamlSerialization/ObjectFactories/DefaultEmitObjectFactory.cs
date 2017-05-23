// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerialization.ObjectFactories
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
#if NetCore
    using System.Linq;
#endif

    using YamlDotNet.Serialization;

    public class DefaultEmitObjectFactory : IObjectFactory
    {
        private readonly Dictionary<Type, Func<object>> _cache =
            new Dictionary<Type, Func<object>>();
#if NetCore
        private static Type[] EmptyTypes { get; } = new Type[0];
#else
        private static Type[] EmptyTypes => Type.EmptyTypes;
#endif

        public object Create(Type type)
        {
            if (!_cache.TryGetValue(type, out Func<object> func))
            {
#if NetCore
                var ti = type.GetTypeInfo();
                if (ti.IsVisible)
                {
                    var ctor = ti.DeclaredConstructors.FirstOrDefault(c => c.GetParameters().Length == 0);
#else
                if (type.IsVisible)
                {
                    var ctor = type.GetConstructor(Type.EmptyTypes);
#endif
                    if (ctor != null)
                    {
                        func = CreateReferenceTypeFactory(ctor);
                    }
#if NetCore
                    else if (ti.IsValueType)
#else
                    else if (type.IsValueType)
#endif
                    {
                        func = CreateValueTypeFactory(type);
                    }
                }
                if (func == null)
                {
                    var typeName = type.FullName;
                    func = () =>
                    {
                        throw new NotSupportedException(typeName);
                    };
                }
                _cache[type] = func;
            }
            return func();
        }

        private static Func<object> CreateReferenceTypeFactory(ConstructorInfo ctor)
        {
            var dm = new DynamicMethod(string.Empty, typeof(object), EmptyTypes);
            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Newobj, ctor);
#if NetCore
            if (ctor.DeclaringType.GetTypeInfo().IsValueType)
#else
            if (ctor.DeclaringType.IsValueType)
#endif
            {
                il.Emit(OpCodes.Box, ctor.DeclaringType);
            }
            il.Emit(OpCodes.Ret);
            return (Func<object>)dm.CreateDelegate(typeof(Func<object>));
        }

        private static Func<object> CreateValueTypeFactory(Type type)
        {
            var dm = new DynamicMethod(string.Empty, typeof(object), EmptyTypes);
            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Initobj, type);
            il.Emit(OpCodes.Box, type);
            il.Emit(OpCodes.Ret);
            return (Func<object>)dm.CreateDelegate(typeof(Func<object>));
        }
    }
}
