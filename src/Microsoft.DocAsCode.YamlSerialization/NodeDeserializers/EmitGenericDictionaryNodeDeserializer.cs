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
    using YamlDotNet.Core.Events;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.YamlSerialization.Helpers;

    public class EmitGenericDictionaryNodeDeserializer : INodeDeserializer
    {
        private static readonly MethodInfo DeserializeHelperMethod =
            typeof(EmitGenericDictionaryNodeDeserializer).GetMethod(nameof(DeserializeHelper));
            private readonly IObjectFactory _objectFactory;
        private readonly Dictionary<Type, Type[]> _gpCache =
            new Dictionary<Type, Type[]>();
        private readonly Dictionary<Tuple<Type, Type>, Action<IParser, Type, Func<IParser, Type, object>, object>> _actionCache =
            new Dictionary<Tuple<Type, Type>, Action<IParser, Type, Func<IParser, Type, object>, object>>();

        public EmitGenericDictionaryNodeDeserializer(IObjectFactory objectFactory)
        {
            _objectFactory = objectFactory;
        }

        bool INodeDeserializer.Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object value)
        {
            if (!_gpCache.TryGetValue(expectedType, out Type[] gp))
            {
                var dictionaryType = ReflectionUtility.GetImplementedGenericInterface(expectedType, typeof(IDictionary<,>));
                if (dictionaryType != null)
                {
                    gp = dictionaryType.GetGenericArguments();
                }
                else
                {
                    dictionaryType = ReflectionUtility.GetImplementedGenericInterface(expectedType, typeof(IReadOnlyDictionary<,>));
                    if (dictionaryType != null)
                    {
                        gp = dictionaryType.GetGenericArguments();
                    }
                }
                _gpCache[expectedType] = gp;
            }

            if (gp == null)
            {
                value = false;
                return false;
            }

            reader.Expect<MappingStart>();

            value = _objectFactory.Create(expectedType);
            var cacheKey = Tuple.Create(gp[0], gp[1]);
            if (!_actionCache.TryGetValue(cacheKey, out var action))
            {
                var dm = new DynamicMethod(string.Empty, typeof(void), new[] { typeof(IParser), typeof(Type), typeof(Func<IParser, Type, object>), typeof(object) });
                var il = dm.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Castclass, typeof(IDictionary<,>).MakeGenericType(gp));
                il.Emit(OpCodes.Call, DeserializeHelperMethod.MakeGenericMethod(gp));
                il.Emit(OpCodes.Ret);
                action = (Action<IParser, Type, Func<IParser, Type, object>, object>)dm.CreateDelegate(typeof(Action<IParser, Type, Func<IParser, Type, object>, object>));
                _actionCache[cacheKey] = action;
            }
            action(reader, expectedType, nestedObjectDeserializer, value);

            reader.Expect<MappingEnd>();

            return true;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void DeserializeHelper<TKey, TValue>(IParser reader, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, IDictionary<TKey, TValue> result)
        {
            while (!reader.Accept<MappingEnd>())
            {
                var key = nestedObjectDeserializer(reader, typeof(TKey));
                var keyPromise = key as IValuePromise;

                var value = nestedObjectDeserializer(reader, typeof(TValue));
                var valuePromise = value as IValuePromise;

                if (keyPromise == null)
                {
                    if (valuePromise == null)
                    {
                        // Happy path: both key and value are known
                        result[(TKey)key] = (TValue)value;
                    }
                    else
                    {
                        // Key is known, value is pending
                        valuePromise.ValueAvailable += v => result[(TKey)key] = (TValue)v;
                    }
                }
                else
                {
                    if (valuePromise == null)
                    {
                        // Key is pending, value is known
                        keyPromise.ValueAvailable += v => result[(TKey)v] = (TValue)value;
                    }
                    else
                    {
                        // Both key and value are pending. We need to wait until both of them becom available.
                        var hasFirstPart = false;

                        keyPromise.ValueAvailable += v =>
                        {
                            if (hasFirstPart)
                            {
                                result[(TKey)v] = (TValue)value;
                            }
                            else
                            {
                                key = v;
                                hasFirstPart = true;
                            }
                        };

                        valuePromise.ValueAvailable += v =>
                        {
                            if (hasFirstPart)
                            {
                                result[(TKey)key] = (TValue)v;
                            }
                            else
                            {
                                value = v;
                                hasFirstPart = true;
                            }
                        };
                    }
                }
            }
        }
    }
}
