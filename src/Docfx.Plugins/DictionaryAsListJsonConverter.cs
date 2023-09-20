// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Docfx.Plugins;

public class DictionaryAsListJsonConverter<T> : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(IList<KeyValuePair<string, T>>);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var value = reader.Value;
        IEnumerable<JToken> jItems;
        if (reader.TokenType == JsonToken.StartObject)
        {
            jItems = JToken.Load(reader);
        }
        else
        {
            throw new JsonReaderException($"{reader.TokenType} is not a valid {objectType.Name}.");
        }

        return jItems.Select(s => ParseItem(s, serializer)).ToList();
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        foreach (var item in ((IList<KeyValuePair<string, T>>)value))
        {
            writer.WritePropertyName(item.Key);
            serializer.Serialize(writer, item.Value);
        }
        writer.WriteEndObject();
    }

    private static KeyValuePair<string, T> ParseItem(JToken item, JsonSerializer serializer)
    {
        if (item.Type == JTokenType.Property)
        {
            JProperty jProperty = item as JProperty;
            var pattern = jProperty.Name;
            var value = jProperty.Value;
            return new KeyValuePair<string, T>(pattern, serializer.Deserialize<T>(value.CreateReader()));
        }
        else
        {
            throw new JsonReaderException($"Unsupported value {item} (type: {item.Type}).");
        }
    }
}
