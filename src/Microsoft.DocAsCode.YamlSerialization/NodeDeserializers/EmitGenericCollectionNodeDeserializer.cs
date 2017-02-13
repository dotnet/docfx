﻿// Copyright (c) Microsoft. All rights reserved.
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

    public class EmitGenericCollectionNodeDeserializer : INodeDeserializer
    {
        private static readonly MethodInfo DeserializeHelperMethod =
#if NetCore
            typeof(EmitGenericCollectionNodeDeserializer).GetTypeInfo().GetDeclaredMethod(nameof(DeserializeHelper));
#else
            typeof(EmitGenericCollectionNodeDeserializer).GetMethod(nameof(DeserializeHelper));
#endif
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
            Type gp;
            if (!_gpCache.TryGetValue(expectedType, out gp))
            {
                var collectionType = ReflectionUtility.GetImplementedGenericInterface(expectedType, typeof(ICollection<>));
                if (collectionType != null)
                {
#if NetCore
                    gp = collectionType.GetTypeInfo().GenericTypeParameters[0];
#else
                    gp = collectionType.GetGenericArguments()[0];
#endif
                }
                _gpCache[expectedType] = gp;
            }
            if (gp == null)
            {
                value = false;
                return false;
            }

            value = _objectFactory.Create(expectedType);
            Action<IParser, Type, Func<IParser, Type, object>, object> action;
            if (!_actionCache.TryGetValue(gp, out action))
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

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
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
