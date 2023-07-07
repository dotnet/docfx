// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Docfx;

[Serializable]
internal class ContentPairingInfo
{
    [JsonProperty("contentFolder")]
    public string ContentFolder { get; set; }

    [JsonProperty("overwriteFragmentsFolder")]
    public string OverwriteFragmentsFolder { get; set; }
}
