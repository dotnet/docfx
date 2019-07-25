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
            var args = new object[genericTypes.Length];

            // Trying to find an exact match first
            for (var i = 0; i < genericTypes.Length; i++)
            {
                if (TypeExactlyMatches(reader.TokenType, genericTypes[i]))
                {
                    args[i] = serializer.Deserialize(reader, genericTypes[i]);
                    return args;
                }
            }

            // Exclude types that never matches
            for (var i = 0; i < genericTypes.Length; i++)
            {
                if (!TypeNeverMatches(reader.TokenType, genericTypes[i]))
                {
                    args[i] = serializer.Deserialize(reader, genericTypes[i]);
                    return args;
                }
            }

            return args;
        }

        private static bool TypeExactlyMatches(JsonToken tokenType, Type objectType)
        {
            switch (tokenType)
            {
                case JsonToken.StartArray:
                    return objectType.IsArray;
                case JsonToken.String:
                    return objectType == typeof(string);
                case JsonToken.Boolean:
                    return objectType == typeof(bool);
                case JsonToken.Integer:
                    return objectType == typeof(int);
                case JsonToken.Float:
                    return objectType == typeof(float);
                default:
                    return false;
            }
        }

        private static bool TypeNeverMatches(JsonToken tokenType, Type objectType)
        {
            return objectType.IsArray && tokenType != JsonToken.StartArray;
        }
    }
}
