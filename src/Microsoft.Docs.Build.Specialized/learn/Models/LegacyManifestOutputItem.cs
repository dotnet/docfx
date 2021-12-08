// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.LearnValidation;

public class LegacyManifestOutputItem
{
    // output path relative to site base path
    [JsonProperty("relative_path")]
    public string RelativePath { get; set; } = "";

    /// <summary>
    /// Gets or sets output absolute path, used when output not within build output directory
    /// e.g. resource's output when <see cref="OutputConfig.SelfContained"/> = false
    /// </summary>
    [JsonProperty("link_to_path")]
    public string LinkToPath { get; set; } = "";
}
