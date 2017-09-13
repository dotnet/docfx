// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class MarkdownSytleDefinition
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
}
