// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Docfx.Plugins;

public class UidDefinition
{
    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonProperty("file")]
    [JsonPropertyName("file")]
    public string File { get; }

    [JsonProperty("line")]
    [JsonPropertyName("line")]
    public int? Line { get; }

    [JsonProperty("column")]
    [JsonPropertyName("column")]
    public int? Column { get; }

    [JsonProperty("path")]
    [JsonPropertyName("path")]
    public string Path { get; }

    [Newtonsoft.Json.JsonConstructor]
    [System.Text.Json.Serialization.JsonConstructor]
    public UidDefinition(string name, string file, int? line = null, int? column = null, string path = null)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Uid name cannot be null or empty.", nameof(name));
        }

        Name = name;
        File = file;
        Line = line;
        Column = column;
        Path = path;
    }
}
