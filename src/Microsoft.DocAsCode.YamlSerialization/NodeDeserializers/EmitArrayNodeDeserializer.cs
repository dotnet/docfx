// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerialization.NodeDeserializers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Reflection;
    using System.Reflection.Emit;

    using YamlDotNet.Core;
    using YamlDotNet.Serialization;

    public class EmitArrayNodeDeserializer : INodeDeserializer
    {
        private static MethodInfo DeserializeHelperMethod =
#if NetCore
            typeof(EmitArrayNodeDeserializer).GetTypeInfo().GetDeclaredMethod(nameof(DeserializeHelper));
#else
            typeof(EmitArrayNodeDeserializer).GetMethod(nameof(DeserializeHelper));
#endif
        private static readonly Dictionary<Type, Func<EventReader, Type, Func<EventReader, Type, object>, object>> _funcCache =
            new Dictionary<Type, Func<EventReader, Type, Func<EventReader, Type, object>, object>>();

        bool INodeDeserializer.Deserialize(EventReader reader, Type expectedType, Func<EventReader, Type, object> nestedObjectDeserializer, out object value)
        {
            if (!expectedType.IsArray)
            {
                value = false;
                return false;
            }

            Func<EventReader, Type, Func<EventReader, Type, object>, object> func;
            if (!_funcCache.TryGetValue(expectedType, out func))
            {
                var dm = new DynamicMethod(string.Empty, typeof(object), new[] { typeof(EventReader), typeof(Type), typeof(Func<EventReader, Type, object>) });
                var il = dm.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Call, DeserializeHelperMethod.MakeGenericMethod(expectedType.GetElementType()));
                il.Emit(OpCodes.Ret);
                func = (Func<EventReader, Type, Func<EventReader, Type, object>, object>)dm.CreateDelegate(typeof(Func<EventReader, Type, Func<EventReader, Type, object>, object>));
                _funcCache[expectedType] = func;
            }
            value = func(reader, expectedType, nestedObjectDeserializer);
            return true;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static TItem[] DeserializeHelper<TItem>(EventReader reader, Type expectedType, Func<EventReader, Type, object> nestedObjectDeserializer)
        {
            var items = new List<TItem>();
            EmitGenericCollectionNodeDeserializer.DeserializeHelper(reader, expectedType, nestedObjectDeserializer, items);
            return items.ToArray();
        }
    }
}
