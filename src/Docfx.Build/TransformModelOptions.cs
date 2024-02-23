// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Docfx.Build.Engine;

public class TransformModelOptions
{
    [JsonPropertyName("isShared")]
    public bool IsShared { get; set; }

    [JsonPropertyName("bookmarks")]
    public Dictionary<string, string> Bookmarks { get; set; }
}
