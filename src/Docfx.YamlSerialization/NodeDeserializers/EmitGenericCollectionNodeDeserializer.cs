// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Emit;
using Docfx.YamlSerialization.Helpers;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Utilities;
using EditorBrowsable = System.ComponentModel.EditorBrowsableAttribute;
using EditorBrowsableState = System.ComponentModel.EditorBrowsableState;

namespace Docfx.YamlSerialization.NodeDeserializers;

public class EmitGenericCollectionNodeDeserializer : INodeDeserializer
{
    private static readonly MethodInfo DeserializeHelperMethod =
        typeof(EmitGenericCollectionNodeDeserializer).GetMethod(nameof(DeserializeHelper))!;
    private readonly IObjectFactory _objectFactory;
    private readonly INamingConvention _enumNamingConvention;
    private readonly ITypeInspector _typeDescriptor;
    private readonly Dictionary<Type, Type?> _gpCache =
        new();
    private readonly Dictionary<Type, Action<IParser, Type, Func<IParser, Type, object?>, object?, INamingConvention, ITypeInspector>> _actionCache =
        new();

    public EmitGenericCollectionNodeDeserializer(IObjectFactory objectFactory, INamingConvention enumNamingConvention, ITypeInspector typeDescriptor)
    {
        _objectFactory = objectFactory;
        _enumNamingConvention = enumNamingConvention;
        _typeDescriptor = typeDescriptor;
    }

    bool INodeDeserializer.Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
    {
        if (!_gpCache.TryGetValue(expectedType, out var gp))
        {
            var collectionType = ReflectionUtility.GetImplementedGenericInterface(expectedType, typeof(ICollection<>));
            if (collectionType != null)
            {
                gp = collectionType.GetGenericArguments()[0];
            }
            else
            {
                collectionType = ReflectionUtility.GetImplementedGenericInterface(expectedType, typeof(IReadOnlyCollection<>));
                if (collectionType != null)
                {
                    gp = collectionType.GetGenericArguments()[0];
                }
            }
            _gpCache[expectedType] = gp;
        }
        if (gp == null)
        {
            value = false;
            return false;
        }

        value = _objectFactory.Create(expectedType);
        if (!_actionCache.TryGetValue(gp, out var action))
        {
            var dm = new DynamicMethod(
                string.Empty,
                returnType: typeof(void),
                [
                    typeof(IParser),
                    typeof(Type),
                    typeof(Func<IParser, Type, object?>),
                    typeof(object),
                    typeof(INamingConvention),
                    typeof(ITypeInspector)
                ]);

            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); // reader
            il.Emit(OpCodes.Ldarg_1); // expectedType
            il.Emit(OpCodes.Ldarg_2); // nestedObjectDeserializer
            il.Emit(OpCodes.Ldarg_3); // result
            il.Emit(OpCodes.Castclass, typeof(ICollection<>).MakeGenericType(gp));
            il.Emit(OpCodes.Ldarg_S, (byte)4); // enumNamingConvention
            il.Emit(OpCodes.Ldarg_S, (byte)5); // typeDescriptor
            il.Emit(OpCodes.Call, DeserializeHelperMethod.MakeGenericMethod(gp));
            il.Emit(OpCodes.Ret);
            action = (Action<IParser, Type, Func<IParser, Type, object?>, object?, INamingConvention, ITypeInspector>)dm.CreateDelegate(typeof(Action<IParser, Type, Func<IParser, Type, object?>, object?, INamingConvention, ITypeInspector>));
            _actionCache[gp] = action;
        }

        action(reader, expectedType, nestedObjectDeserializer, value, _enumNamingConvention, _typeDescriptor);
        return true;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void DeserializeHelper<TItem>(
        IParser reader,
        Type expectedType,
        Func<IParser, Type, object?> nestedObjectDeserializer,
        ICollection<TItem> result,
        INamingConvention enumNamingConvention,
        ITypeInspector typeDescriptor)
    {
        reader.Consume<SequenceStart>();
        while (!reader.Accept<SequenceEnd>(out _))
        {
            var value = nestedObjectDeserializer(reader, typeof(TItem));
            if (value is not IValuePromise promise)
            {
                result.Add(TypeConverter.ChangeType<TItem>(value, enumNamingConvention, typeDescriptor));
            }
            else if (result is IList<TItem> list)
            {
                var index = list.Count;
                result.Add(default!);
                promise.ValueAvailable += v => list[index] = TypeConverter.ChangeType<TItem>(v, enumNamingConvention, typeDescriptor);
            }
            else
            {
                var current = reader.Current!;
                throw new ForwardAnchorNotSupportedException(
                    current.Start,
                    current.End,
                    "Forward alias references are not allowed because this type does not implement IList<>"
                );
            }
        }
        reader.Consume<SequenceEnd>();
    }
}
