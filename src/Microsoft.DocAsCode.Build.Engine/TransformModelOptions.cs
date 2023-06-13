// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Microsoft.DocAsCode.Build.Engine;

public class TransformModelOptions
{
    [JsonProperty(PropertyName = "isShared")]
    public bool IsShared { get; set; }

    [JsonProperty(PropertyName = "bookmarks")]
    public Dictionary<string, string> Bookmarks { get; set; }
}
