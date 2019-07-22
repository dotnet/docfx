// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class UnionTypeConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new InvalidOperationException();

        public override bool CanConvert(Type objectType) => typeof(ITuple).IsAssignableFrom(objectType);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var genericTypes = objectType.GetGenericArguments();
            var args = new object[genericTypes.Length];

            for (var i = 0; i < args.Length; i++)
            {
                if (TypeMatches(reader.TokenType, genericTypes[i]))
                {
                    args[i] = serializer.Deserialize(reader, genericTypes[i]);
                    break;
                }
            }

            return Activator.CreateInstance(objectType, args);
        }

        private static bool TypeMatches(JsonToken tokenType, Type type)
        {
            if (type == typeof(string))
                return tokenType == JsonToken.String;
            if (type == typeof(bool))
                return tokenType == JsonToken.Boolean;
            if (type == typeof(int))
                return tokenType == JsonToken.Integer;
            if (type == typeof(float) || type == typeof(double))
                return tokenType == JsonToken.Float;
            if (type.IsArray)
                return tokenType == JsonToken.StartArray;

            return tokenType == JsonToken.StartObject;
        }
    }
}
