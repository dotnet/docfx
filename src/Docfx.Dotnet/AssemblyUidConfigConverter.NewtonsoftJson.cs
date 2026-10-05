// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Docfx;

internal partial class AssemblyUidConfigConverter
{
    /// <summary>
    /// JsonConverter for <see cref="AssemblyUidConfig"/>.
    /// </summary>
    internal class NewtonsoftJsonConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(AssemblyUidConfig);
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.StartArray:
                    return new AssemblyUidConfig(JArray.Load(reader).Select(item => item.ToString()));

                case JsonToken.StartObject:
                    var result = new AssemblyUidConfig();
                    foreach (var property in JObject.Load(reader).Properties())
                    {
                        result[property.Name] = property.Value.Type is JTokenType.Null ? null : property.Value.ToString();
                    }
                    return result;

                default:
                    throw new JsonSerializationException($"TokenType({reader.TokenType}) is not supported.");
            }
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var assemblyUids = (AssemblyUidConfig)value;

            // An array is the shape this was most likely authored in, so it is the shape it round trips
            // to, unless a component was named explicitly.
            if (assemblyUids.Values.All(component => component is null))
            {
                writer.WriteStartArray();
                foreach (var assemblyName in assemblyUids.Keys)
                {
                    writer.WriteValue(assemblyName);
                }
                writer.WriteEndArray();
                return;
            }

            writer.WriteStartObject();
            foreach (var (assemblyName, component) in assemblyUids)
            {
                writer.WritePropertyName(assemblyName);
                writer.WriteValue(component);
            }
            writer.WriteEndObject();
        }
    }
}
