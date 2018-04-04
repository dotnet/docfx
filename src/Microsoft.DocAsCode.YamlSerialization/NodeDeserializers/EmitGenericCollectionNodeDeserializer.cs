// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerialization.NodeDeserializers
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    using YamlDotNet.Core;
    using YamlDotNet.Core.Events;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.Utilities;

    using Microsoft.DocAsCode.YamlSerialization.Helpers;

    using EditorBrowsable = System.ComponentModel.EditorBrowsableAttribute;
    using EditorBrowsableState = System.ComponentModel.EditorBrowsableState;

    public class EmitGenericCollectionNodeDeserializer : INodeDeserializer
    {
        private static readonly MethodInfo DeserializeHelperMethod =
            typeof(EmitGenericCollectionNodeDeserializer).GetMethod(nameof(DeserializeHelper));
            private readonly IObjectFactory _objectFactory;
        private readonly Dictionary<Type, Type> _gpCache =
            new Dictionary<Type, Type>();
        private readonly Dictionary<Type, Action<IParser, Type, Func<IParser, Type, object>, object>> _actionCache =
            new Dictionary<Type, Action<IParser, Type, Func<IParser, Type, object>, object>>();

        public EmitGenericCollectionNodeDeserializer(IObjectFactory objectFactory)
        {
            _objectFactory = objectFactory;
        }

        bool INodeDeserializer.Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object value)
        {
            if (!_gpCache.TryGetValue(expectedType, out Type gp))
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
                var dm = new DynamicMethod(string.Empty, typeof(void), new[] { typeof(IParser), typeof(Type), typeof(Func<IParser, Type, object>), typeof(object) });
                var il = dm.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Castclass, typeof(ICollection<>).MakeGenericType(gp));
                il.Emit(OpCodes.Call, DeserializeHelperMethod.MakeGenericMethod(gp));
                il.Emit(OpCodes.Ret);
                action = (Action<IParser, Type, Func<IParser, Type, object>, object>)dm.CreateDelegate(typeof(Action<IParser, Type, Func<IParser, Type, object>, object>));
                _actionCache[gp] = action;
            }

            action(reader, expectedType, nestedObjectDeserializer, value);
            return true;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void DeserializeHelper<TItem>(IParser reader, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, ICollection<TItem> result)
        {
            var list = result as IList<TItem>;

            reader.Expect<SequenceStart>();
            while (!reader.Accept<SequenceEnd>())
            {
                var current = reader.Current;

                var value = nestedObjectDeserializer(reader, typeof(TItem));
                var promise = value as IValuePromise;
                if (promise == null)
                {
                    result.Add(TypeConverter.ChangeType<TItem>(value));
                }
                else if (list != null)
                {
                    var index = list.Count;
                    result.Add(default(TItem));
                    promise.ValueAvailable += v => list[index] = TypeConverter.ChangeType<TItem>(v);
                }
                else
                {
                    throw new ForwardAnchorNotSupportedException(
                        current.Start,
                        current.End,
                        "Forward alias references are not allowed because this type does not implement IList<>"
                    );
                }
            }
            reader.Expect<SequenceEnd>();
        }
    }
}
