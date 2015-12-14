// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerializations.ObjectFactories
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    using YamlDotNet.Serialization;

    public class DefaultEmitObjectFactory : IObjectFactory
    {
        private readonly Dictionary<Type, Func<object>> _cache =
            new Dictionary<Type, Func<object>>();

        public object Create(Type type)
        {
            Func<object> func;
            if (!_cache.TryGetValue(type, out func))
            {
                if (type.IsVisible)
                {
                    var ctor = type.GetConstructor(Type.EmptyTypes);
                    if (ctor != null)
                    {
                        func = CreateReferenceTypeFactory(ctor);
                    }
                    else if (type.IsValueType)
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
            var dm = new DynamicMethod(string.Empty, typeof(object), Type.EmptyTypes);
            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Newobj, ctor);
            if (ctor.DeclaringType.IsValueType)
            {
                il.Emit(OpCodes.Box, ctor.DeclaringType);
            }
            il.Emit(OpCodes.Ret);
            return (Func<object>)dm.CreateDelegate(typeof(Func<object>));
        }

        private static Func<object> CreateValueTypeFactory(Type type)
        {
            var dm = new DynamicMethod(string.Empty, typeof(object), Type.EmptyTypes);
            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Initobj, type);
            il.Emit(OpCodes.Box, type);
            il.Emit(OpCodes.Ret);
            return (Func<object>)dm.CreateDelegate(typeof(Func<object>));
        }
    }
}
