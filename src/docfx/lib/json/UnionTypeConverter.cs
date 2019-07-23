// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// JsonConverter that converts a tuple based on whether input JSON is a scalar, array or object.
    /// </summary>
    internal class UnionTypeConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new InvalidOperationException();

        public override bool CanConvert(Type objectType) => typeof(ITuple).IsAssignableFrom(objectType);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var args = ReadJsonCore(reader, objectType, serializer);

            return Activator.CreateInstance(objectType, args);
        }

        private static object[] ReadJsonCore(JsonReader reader, Type objectType, JsonSerializer serializer)
        {
            var genericTypes = objectType.GetGenericArguments();
            var i = 0;

            switch (reader.TokenType)
            {
                case JsonToken.StartArray:
                    for (i = 0; i < genericTypes.Length; i++)
                    {
                        if (genericTypes[i].IsArray)
                        {
                            break;
                        }
                    }
                    break;

                case JsonToken.StartObject:
                    for (i = 0; i < genericTypes.Length; i++)
                    {
                        if (genericTypes[i].IsPrimitive || genericTypes[i] == typeof(string))
                        {
                            break;
                        }
                    }
                    break;
            }

            if (i == genericTypes.Length)
            {
                i = 0;
            }

            var args = new object[genericTypes.Length];
            args[i] = serializer.Deserialize(reader, genericTypes[i]);
            return args;
        }
    }
}
