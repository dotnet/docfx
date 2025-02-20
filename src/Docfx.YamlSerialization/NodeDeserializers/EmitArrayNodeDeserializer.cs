// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Docfx.YamlSerialization.NodeDeserializers;

public class EmitArrayNodeDeserializer : INodeDeserializer
{
    private readonly INamingConvention _enumNamingConvention;
    private readonly ITypeInspector _typeDescriptor;

    private static readonly MethodInfo DeserializeHelperMethod =
        typeof(EmitArrayNodeDeserializer).GetMethod(nameof(DeserializeHelper))!;

    private static readonly ConcurrentDictionary<Type, Func<IParser, Type, Func<IParser, Type, object?>, INamingConvention, ITypeInspector, object?>> _funcCache =
        new();

    public EmitArrayNodeDeserializer(INamingConvention enumNamingConvention, ITypeInspector typeDescriptor)
    {
        _enumNamingConvention = enumNamingConvention;
        _typeDescriptor = typeDescriptor;
    }

    bool INodeDeserializer.Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
    {
        if (!expectedType.IsArray)
        {
            value = false;
            return false;
        }

        var func = _funcCache.GetOrAdd(expectedType, AddItem);
        value = func(reader, expectedType, nestedObjectDeserializer, _enumNamingConvention, _typeDescriptor);
        return true;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static TItem[] DeserializeHelper<TItem>(
        IParser reader,
        Type expectedType,
        Func<IParser, Type, object?> nestedObjectDeserializer,
        INamingConvention enumNamingConvention,
        ITypeInspector typeDescriptor)
    {
        var items = new List<TItem>();
        EmitGenericCollectionNodeDeserializer.DeserializeHelper(reader, expectedType, nestedObjectDeserializer, items, enumNamingConvention, typeDescriptor);
        return items.ToArray();
    }

    private static Func<IParser, Type, Func<IParser, Type, object?>, INamingConvention, ITypeInspector, object?> AddItem(Type expectedType)
    {
        var dm = new DynamicMethod(
            string.Empty,
            returnType: typeof(object),
            parameterTypes:
            [
                typeof(IParser),                      // reader
                typeof(Type),                         // expectedType
                typeof(Func<IParser, Type, object?>), // nestedObjectDeserializer
                typeof(INamingConvention),            // enumNamingConvention 
                typeof(ITypeInspector),               // typeDescriptor
            ]);
        var il = dm.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Ldarg_3);
        il.Emit(OpCodes.Ldarg_S, (byte)4);
        il.Emit(OpCodes.Call, DeserializeHelperMethod.MakeGenericMethod(expectedType.GetElementType()!));
        il.Emit(OpCodes.Ret);
        return (Func<IParser, Type, Func<IParser, Type, object?>, INamingConvention, ITypeInspector, object?>)dm.CreateDelegate(typeof(Func<IParser, Type, Func<IParser, Type, object?>, INamingConvention, ITypeInspector, object?>));
    }
}
