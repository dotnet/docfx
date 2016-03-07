// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tools.AzureMarkdownRewriterTool
{
    using Newtonsoft.Json;

    public class AzureTransformArguments
    {
        /// <summary>
        /// source directory
        /// </summary>
        [JsonProperty("source_dir")]
        public string SourceDir { get; set; }

        /// <summary>
        /// dest directory
        /// </summary>
        [JsonProperty("dest_dir")]
        public string DestDir { get; set; }

        /// <summary>
        /// Docs document uri prefix
        /// </summary>
        [JsonProperty("docs_host_uri_prefix")]
        public string DocsHostUriPrefix { get; set; }
    }
}
