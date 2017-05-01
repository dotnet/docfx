// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using Newtonsoft.Json;

    internal class CommandMessage
    {
        [JsonProperty("name")]
        public CommandName Name { get; set; }

        [JsonProperty("relativePath")]
        public string RelativePath { get; set; }

        [JsonProperty("workspacePath")]
        public string WorkspacePath { get; set; }

        [JsonProperty("markdownContent")]
        public string MarkdownContent { get; set; }

        [JsonProperty("shouldSeparateMarkupResult")]
        public bool ShouldSeparateMarkupResult { get; set; }

        [JsonProperty("tempPreviewFilePath")]
        public string TempPreviewFilePath { get; set; }

        [JsonProperty("pageRefreshJsFilePath")]
        public string PageRefreshJsFilePath { get; set; }

        [JsonProperty("originalHtmlPath")]
        public string OriginalHtmlPath { get; set; }

        [JsonProperty("navigationPort")]
        public string NavigationPort { get; set; }
    }
}
