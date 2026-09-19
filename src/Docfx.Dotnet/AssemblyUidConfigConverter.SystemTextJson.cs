// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Docfx;

internal partial class AssemblyUidConfigConverter
{
    /// <summary>
    /// JsonConverter for <see cref="AssemblyUidConfig"/>.
    /// </summary>
    internal class SystemTextJsonConverter : JsonConverter<AssemblyUidConfig>
    {
        public override AssemblyUidConfig Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartArray:
                    return new AssemblyUidConfig(JsonSerializer.Deserialize<string[]>(ref reader, options));

                case JsonTokenType.StartObject:
                    return new AssemblyUidConfig(JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options));

                default:
                    throw new JsonException($"TokenType({reader.TokenType}) is not supported.");
            }
        }

        public override void Write(Utf8JsonWriter writer, AssemblyUidConfig value, JsonSerializerOptions options)
        {
            // An array is the shape this was most likely authored in, so it is the shape it round trips
            // to, unless a component was named explicitly.
            if (value.Values.All(component => component is null))
            {
                writer.WriteStartArray();
                foreach (var assemblyName in value.Keys)
                {
                    writer.WriteStringValue(assemblyName);
                }
                writer.WriteEndArray();
                return;
            }

            writer.WriteStartObject();
            foreach (var (assemblyName, component) in value)
            {
                writer.WritePropertyName(assemblyName);
                writer.WriteStringValue(component);
            }
            writer.WriteEndObject();
        }
    }
}
