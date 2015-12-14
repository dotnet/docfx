// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerializations.NodeDeserializers
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    using YamlDotNet.Core;
    using YamlDotNet.Core.Events;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.Utilities;

    using Microsoft.DocAsCode.YamlSerializations.Helpers;

    public class EmitGenericCollectionNodeDeserializer : INodeDeserializer
    {
        private static readonly MethodInfo DeserializeHelperMethod =
            typeof(EmitGenericCollectionNodeDeserializer).GetMethod(nameof(DeserializeHelper));
        private readonly IObjectFactory _objectFactory;
        private readonly Dictionary<Type, Type> _gpCache =
            new Dictionary<Type, Type>();
        private readonly Dictionary<Type, Action<EventReader, Type, Func<EventReader, Type, object>, object>> _actionCache =
            new Dictionary<Type, Action<EventReader, Type, Func<EventReader, Type, object>, object>>();

        public EmitGenericCollectionNodeDeserializer(IObjectFactory objectFactory)
        {
            _objectFactory = objectFactory;
        }

        bool INodeDeserializer.Deserialize(EventReader reader, Type expectedType, Func<EventReader, Type, object> nestedObjectDeserializer, out object value)
        {
            Type gp;
            if (!_gpCache.TryGetValue(expectedType, out gp))
            {
                var collectionType = ReflectionUtility.GetImplementedGenericInterface(expectedType, typeof(ICollection<>));
                if (collectionType != null)
                {
                    gp = collectionType.GetGenericArguments()[0];
                }
                _gpCache[expectedType] = gp;
            }
            if (gp == null)
            {
                value = false;
                return false;
            }

            value = _objectFactory.Create(expectedType);
            Action<EventReader, Type, Func<EventReader, Type, object>, object> action;
            if (!_actionCache.TryGetValue(gp, out action))
            {
                var dm = new DynamicMethod(string.Empty, typeof(void), new[] { typeof(EventReader), typeof(Type), typeof(Func<EventReader, Type, object>), typeof(object) });
                var il = dm.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Castclass, typeof(ICollection<>).MakeGenericType(gp));
                il.Emit(OpCodes.Call, DeserializeHelperMethod.MakeGenericMethod(gp));
                il.Emit(OpCodes.Ret);
                action = (Action<EventReader, Type, Func<EventReader, Type, object>, object>)dm.CreateDelegate(typeof(Action<EventReader, Type, Func<EventReader, Type, object>, object>));
                _actionCache[gp] = action;
            }

            action(reader, expectedType, nestedObjectDeserializer, value);
            return true;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static void DeserializeHelper<TItem>(EventReader reader, Type expectedType, Func<EventReader, Type, object> nestedObjectDeserializer, ICollection<TItem> result)
        {
            var list = result as IList<TItem>;

            reader.Expect<SequenceStart>();
            while (!reader.Accept<SequenceEnd>())
            {
                var current = reader.Parser.Current;

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
