// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Docfx.Plugins;

public class MarkdownStyleConfig
{
    public const string MarkdownStyleFileName = "md.style";

    [JsonProperty("metadataRules")]
    public MarkdownMetadataValidationRule[] MetadataRules { get; set; }
    [JsonProperty("rules")]
    public MarkdownValidationRule[] Rules { get; set; }
    [JsonProperty("tagRules")]
    public MarkdownTagValidationRule[] TagRules { get; set; }
    [JsonProperty("settings")]
    public MarkdownValidationSetting[] Settings { get; set; }
}
