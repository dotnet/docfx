// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerialization.NodeDeserializers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Collections.Concurrent;

    using YamlDotNet.Core;
    using YamlDotNet.Serialization;

    public class EmitArrayNodeDeserializer : INodeDeserializer
    {
        private static MethodInfo DeserializeHelperMethod =
            typeof(EmitArrayNodeDeserializer).GetMethod(nameof(DeserializeHelper));
        private static readonly ConcurrentDictionary<Type, Func<IParser, Type, Func<IParser, Type, object>, object>> _funcCache =
            new ConcurrentDictionary<Type, Func<IParser, Type, Func<IParser, Type, object>, object>>();

        bool INodeDeserializer.Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object value)
        {
            if (!expectedType.IsArray)
            {
                value = false;
                return false;
            }

            var func = _funcCache.GetOrAdd(expectedType, AddItem);
            value = func(reader, expectedType, nestedObjectDeserializer);
            return true;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static TItem[] DeserializeHelper<TItem>(IParser reader, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer)
        {
            var items = new List<TItem>();
            EmitGenericCollectionNodeDeserializer.DeserializeHelper(reader, expectedType, nestedObjectDeserializer, items);
            return items.ToArray();
        }

        private static Func<IParser, Type, Func<IParser, Type, object>, object> AddItem(Type expectedType)
        {
            var dm = new DynamicMethod(string.Empty, typeof(object), new[] { typeof(IParser), typeof(Type), typeof(Func<IParser, Type, object>) });
            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, DeserializeHelperMethod.MakeGenericMethod(expectedType.GetElementType()));
            il.Emit(OpCodes.Ret);
            return (Func<IParser, Type, Func<IParser, Type, object>, object>)dm.CreateDelegate(typeof(Func<IParser, Type, Func<IParser, Type, object>, object>));
        }
    }
}
