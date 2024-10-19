// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Docfx.Common;
using Docfx.Glob;

namespace Docfx;

/// <summary>
/// Glob/Value pair to define define file's metadata.
/// </summary>
/// <see cref="FileMetadataPairs"/>
internal class FileMetadataPairsItem
{
    /// <summary>
    /// The glob pattern to match the files.
    /// </summary>
    public GlobMatcher Glob { get; }

    /// <summary>
    /// JObject, no need to transform it to object as the metadata value will not be used but only to be serialized
    /// </summary>
    [System.Text.Json.Serialization.JsonConverter(typeof(JsonElementConverter))]
    public object Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileMetadataPairsItem"/> class.
    /// </summary>
    public FileMetadataPairsItem(string pattern, object value)
    {
        Glob = new GlobMatcher(pattern);
        Value = ConvertToObjectHelper.ConvertJObjectToObject(value);
    }
}

internal class JsonElementConverter : JsonConverter<JsonElement>
{
    public override JsonElement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, JsonElement value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
