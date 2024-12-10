// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Docfx.MarkdigEngine.Extensions;

internal partial class MarkdigExtensionSettingConverter
{
    internal class NewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(MarkdigExtensionSetting);
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // var value = reader.Value;
            switch (reader.TokenType)
            {
                case JsonToken.String:
                    {
                        var name = (string)reader.Value;
                        return new MarkdigExtensionSetting(name);
                    }
                case JsonToken.StartObject:
                    {
                        var jObj = JObject.Load(reader);

                        var props = jObj.Properties().ToArray();

                        // Object key must be the name of markdig extension.
                        if (props.Length != 1)
                            return null;

                        var prop = props[0];
                        var name = prop.Name;

                        var options = prop.Value;
                        if (options.Count() == 0)
                        {
                            return new MarkdigExtensionSetting(name);
                        }

                        // Convert JToken to JsonElement.
                        var json = options.ToString(Formatting.None);
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        var jsonElement = doc.RootElement.Clone();

                        return new MarkdigExtensionSetting(name)
                        {
                            Options = jsonElement,
                        };
                    }

                default:
                    return null;
            }
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
                return;

            var model = (MarkdigExtensionSetting)value;

            if (model.Options == null || !model.Options.HasValue)
            {
                writer.WriteValue(model.Name);
            }
            else
            {
                writer.WriteStartObject();
                writer.WritePropertyName(model.Name);
                var json = System.Text.Json.JsonSerializer.Serialize(model.Options, DefaultSerializerOptions);
                writer.WriteRawValue(json);
                writer.WriteEndObject();
            }
        }
    }
}

