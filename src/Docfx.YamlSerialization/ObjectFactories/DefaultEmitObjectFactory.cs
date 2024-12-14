// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Emit;

using YamlDotNet.Serialization.ObjectFactories;

namespace Docfx.YamlSerialization.ObjectFactories;

public class DefaultEmitObjectFactory : ObjectFactoryBase
{
    private readonly Dictionary<Type, Func<object>> _cache = [];
    private static Type[] EmptyTypes => Type.EmptyTypes;

    public override object Create(Type type)
    {
        if (!_cache.TryGetValue(type, out var func))
        {
            var realType = type;
            if (type is { IsInterface: true, IsGenericType: true })
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
            else
            {
                throw new InvalidOperationException($"Failed to gets type instance create func for type: {type.FullName}.");
            }

            _cache[type] = func!;
        }
        return func();
    }

    private static Func<object> CreateReferenceTypeFactory(ConstructorInfo ctor)
    {
        var dm = new DynamicMethod(string.Empty, typeof(object), EmptyTypes);
        var il = dm.GetILGenerator();
        il.Emit(OpCodes.Newobj, ctor);
        if (ctor.DeclaringType!.IsValueType)
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
