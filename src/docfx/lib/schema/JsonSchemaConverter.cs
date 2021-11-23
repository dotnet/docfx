// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.Build;

internal class JsonSchemaConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => objectType == typeof(JsonSchema);

    public override bool CanWrite => false;

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Boolean)
        {
            return reader.Value is true ? JsonSchema.TrueSchema : JsonSchema.FalseSchema;
        }

        var result = new JsonSchema();
        serializer.Populate(reader, result);
        result.SchemaResolver = JsonSchemaResolver.Current;
        return result;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) => throw new NotSupportedException();
}
