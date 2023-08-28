// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Docfx;

/// <summary>
/// Content pairing information used for <see cref="BuildJsonConfig.Pairing"/> options.
/// </summary>
[Serializable]
public class ContentPairingInfo
{
    /// <summary>
    /// Content folder.
    /// </summary>
    [JsonProperty("contentFolder")]
    public string ContentFolder { get; set; }

    /// <summary>
    /// Overwrite fragment files folder.
    /// </summary>
    [JsonProperty("overwriteFragmentsFolder")]
    public string OverwriteFragmentsFolder { get; set; }
}
