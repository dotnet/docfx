// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Docfx.Common;

public class JObjectDictionaryToObjectDictionaryConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Dictionary<string, object>);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);
        var converted = ConvertToObjectHelper.ConvertJObjectToObject(jObject);
        return converted;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        foreach (var item in (Dictionary<string, object>)value)
        {
            writer.WritePropertyName(item.Key);
            serializer.Serialize(writer, item.Value);
        }
        writer.WriteEndObject();
    }
}
