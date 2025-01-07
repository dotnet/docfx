// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Docfx.MarkdigEngine.Extensions;

internal partial class MarkdigExtensionSettingConverter
{
    internal class SystemTextJsonConverter : JsonConverter<MarkdigExtensionSetting>
    {
        public override MarkdigExtensionSetting Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    {
                        var name = reader.GetString();
                        return new MarkdigExtensionSetting(name);
                    }
                case JsonTokenType.StartObject:
                    {
                        var elem = JsonElement.ParseValue(ref reader);

                        var props = elem.EnumerateObject().ToArray();

                        // Object key must be the name of markdig extension.
                        if (props.Length != 1)
                            return null;

                        var prop = props[0];
                        var name = prop.Name;
                        var value = prop.Value;

                        if (value.ValueKind != JsonValueKind.Object)
                        {
                            return new MarkdigExtensionSetting(name);
                        }

                        return new MarkdigExtensionSetting(name)
                        {
                            Options = value,
                        };
                    }
                default:
                    throw new JsonException($"TokenType({reader.TokenType}) is not supported.");
            }
        }

        public override void Write(Utf8JsonWriter writer, MarkdigExtensionSetting value, JsonSerializerOptions options)
        {
            if (value == null)
                return;

            var model = (MarkdigExtensionSetting)value;

            if (model.Options == null || !model.Options.HasValue)
            {
                writer.WriteStringValue(model.Name);
            }
            else
            {
                writer.WriteStartObject();
                writer.WritePropertyName(model.Name);
                var json = JsonSerializer.Serialize(model.Options, DefaultSerializerOptions);
                writer.WriteRawValue(json);
                writer.WriteEndObject();
            }
        }
    }
}
