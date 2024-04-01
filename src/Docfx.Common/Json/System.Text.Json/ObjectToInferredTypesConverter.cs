// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

#nullable enable

namespace Docfx.Common;

/// <summary>
/// Custom JsonConverters for <see cref="object"/>.
/// </summary>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/converters-how-to?pivots=dotnet-8-0#deserialize-inferred-types-to-object-properties" />
/// <seealso href="https://github.com/dotnet/runtime/issues/98038" />
internal class ObjectToInferredTypesConverter : JsonConverter<object>
{
    /// <inheritdoc/>
    public override object? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.Number when reader.TryGetInt32(out int intValue):
                return intValue;
            case JsonTokenType.Number when reader.TryGetInt64(out long longValue):
                return longValue;
            case JsonTokenType.Number:
                return reader.GetDouble();
            case JsonTokenType.String when reader.TryGetDateTime(out DateTime datetime):
                return datetime;
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.StartArray:
                {
                    var list = new List<object?>();
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        object? element = Read(ref reader, typeof(object), options);
                        list.Add(element);
                    }
                    return list;
                }
            case JsonTokenType.StartObject:
                {
                    try
                    {
                        using var doc = JsonDocument.ParseValue(ref reader);
                        return JsonSerializer.Deserialize<Dictionary<string, dynamic>>(doc, options);
                    }
                    catch (Exception)
                    {
                        goto default;
                    }
                }
            default:
                {
                    using var doc = JsonDocument.ParseValue(ref reader);
                    return doc.RootElement.Clone();
                }
        }
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, object objectToWrite, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, objectToWrite, objectToWrite.GetType(), options);
    }
}
