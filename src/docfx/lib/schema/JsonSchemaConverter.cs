// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class JsonSchemaConverter : JsonConverter
    {
        [ThreadStatic]
        private static Action<JToken, JsonSchema>? t_onJsonSchema;

        public static Action<JToken, JsonSchema>? OnJsonSchema
        {
            get => t_onJsonSchema;
            set => t_onJsonSchema = value;
        }

        public override bool CanConvert(Type objectType) => objectType == typeof(JsonSchema);

        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var result = ReadJsonCore(reader, serializer);

            if (reader is JTokenReader jTokenReader && jTokenReader.CurrentToken is JToken token)
            {
                t_onJsonSchema?.Invoke(token, result);
            }

            return result;
        }

        private static JsonSchema ReadJsonCore(JsonReader reader, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Boolean)
            {
                return reader.Value is true ? JsonSchema.TrueSchema : JsonSchema.FalseSchema;
            }

            var result = new JsonSchema();
            serializer.Populate(reader, result);
            return result;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) => throw new NotSupportedException();
    }
}
