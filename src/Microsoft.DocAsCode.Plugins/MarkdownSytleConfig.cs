// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using Newtonsoft.Json;

    public class MarkdownSytleConfig
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
}
