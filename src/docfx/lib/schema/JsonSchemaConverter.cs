// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class JsonSchemaConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(JsonSchema);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Boolean)
            {
                return reader.ReadAsBoolean() == true ? JsonSchema.TrueSchema : JsonSchema.FalseSchema;
            }
            return serializer.Deserialize(reader, objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == JsonSchema.TrueSchema)
            {
                writer.WriteValue(true);
            }
            else if (value == JsonSchema.FalseSchema)
            {
                writer.WriteValue(false);
            }
            else
            {
                serializer.Serialize(writer, value);
            }
        }
    }
}