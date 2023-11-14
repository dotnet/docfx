// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Docfx.Build.Engine;

public class TransformModelOptions
{
    [JsonProperty(PropertyName = "isShared")]
    [JsonPropertyName("isShared")]
    public bool IsShared { get; set; }

    [JsonProperty(PropertyName = "bookmarks")]
    [JsonPropertyName("bookmarks")]
    public Dictionary<string, string> Bookmarks { get; set; }
}
