// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Docfx.Plugins;

public class UidDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("file")]
    public string File { get; }

    [JsonPropertyName("line")]
    public int? Line { get; }

    [JsonPropertyName("column")]
    public int? Column { get; }

    [JsonPropertyName("path")]
    public string Path { get; }

    [JsonConstructor]
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
