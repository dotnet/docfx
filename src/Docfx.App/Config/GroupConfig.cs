// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Docfx;

/// <summary>
/// Group configuration.
/// </summary>
internal class GroupConfig
{
    /// <summary>
    /// Defines the output folder of the generated build files.
    /// </summary>
    [JsonProperty("dest")]
    [JsonPropertyName("dest")]
    public string Destination { get; set; }

    /// <summary>
    /// Extension metadata.
    /// </summary>
    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = [];
}
