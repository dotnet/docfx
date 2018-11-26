// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerialization.ObjectFactories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    using YamlDotNet.Serialization;

    public class DefaultEmitObjectFactory : IObjectFactory
    {
        private readonly Dictionary<Type, Func<object>> _cache =
            new Dictionary<Type, Func<object>>();
        private static Type[] EmptyTypes => Type.EmptyTypes;

        public object Create(Type type)
        {
            if (!_cache.TryGetValue(type, out Func<object> func))
            {
                if (type.IsVisible)
                {
                    var realType = type;
                    if (type.IsInterface && type.IsGenericType)
                    {
                        var def = type.GetGenericTypeDefinition();
                        var args = type.GetGenericArguments();
                        if (def == typeof(IDictionary<,>) || def == typeof(IReadOnlyDictionary<,>))
                        {
                            realType = typeof(Dictionary<,>).MakeGenericType(args);
                        }
                        if (def == typeof(IList<>) || def == typeof(IReadOnlyList<>) ||
                            def == typeof(ICollection<>) || def == typeof(IReadOnlyCollection<>) ||
                            def == typeof(IEnumerable<>))
                        {
                            realType = typeof(List<>).MakeGenericType(args);
                        }
                        if (def == typeof(ISet<>))
                        {
                            realType = typeof(HashSet<>).MakeGenericType(args);
                        }
                    }
                    var ctor = realType.GetConstructor(Type.EmptyTypes);
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
                    func = () => throw new NotSupportedException(typeName);
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
            if (ctor.DeclaringType.IsValueType)
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
