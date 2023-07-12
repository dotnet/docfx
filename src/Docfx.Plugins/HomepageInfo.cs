// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Docfx.Plugins;

public class HomepageInfo
{
    [JsonProperty("tocPath")]
    public string TocPath { get; set; }

    [JsonProperty("homepage")]
    public string Homepage { get; set; }
}
