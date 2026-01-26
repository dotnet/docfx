// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

#nullable enable

namespace Docfx;

internal class CleanJsonConfig
{
    /// <summary>
    /// If set to true, skip file/directory delete operations.
    /// </summary>
    [JsonProperty("dryRun")]
    [JsonPropertyName("dryRun")]
    public bool? DryRun { get; set; }
}
