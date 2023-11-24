// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Docfx;

/// <summary>
/// Holds configuration options specific to the wkhtmltopdf tooling used by the pdf command.
/// </summary>
internal class WkhtmltopdfJsonConfig
{
    /// <summary>
    /// Gets or sets the path and file name of a wkhtmltopdf.exe compatible executable.
    /// </summary>
    [JsonProperty("filePath")]
    [JsonPropertyName("filePath")]
    public string FilePath { get; set; }

    /// <summary>
    /// Specify additional command line arguments that should be passed to the wkhtmltopdf executable.
    /// </summary>
    [JsonProperty("additionalArguments")]
    [JsonPropertyName("additionalArguments")]
    public string AdditionalArguments { get; set; }
}
