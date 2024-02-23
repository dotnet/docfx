// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Docfx;

/// <summary>
/// Group configuration.
/// </summary>
internal class GroupConfig
{
    /// <summary>
    /// Defines the output folder of the generated build files.
    /// </summary>
    [JsonPropertyName("dest")]
    public string Destination { get; set; }

    /// <summary>
    /// Extension metadata.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
