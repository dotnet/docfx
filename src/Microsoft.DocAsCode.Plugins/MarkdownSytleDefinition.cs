// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Microsoft.DocAsCode.Plugins;

public class MarkdownStyleDefinition
{
    public const string MarkdownStyleDefinitionFilePostfix = ".md.style";
    public const string MarkdownStyleDefinitionFolderName = "md.styles";

    [JsonProperty("metadataRules")]
    public Dictionary<string, MarkdownMetadataValidationRule> MetadataRules { get; set; }
    [JsonProperty("rules")]
    public Dictionary<string, MarkdownValidationRule> Rules { get; set; }
    [JsonProperty("tagRules")]
    public Dictionary<string, MarkdownTagValidationRule> TagRules { get; set; }
}
