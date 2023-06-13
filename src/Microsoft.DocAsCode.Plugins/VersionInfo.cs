// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Microsoft.DocAsCode.Plugins;

public class VersionInfo
{
    [JsonProperty("version_folder")]
    public string VersionFolder { get; set; }
}
