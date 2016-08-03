// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tools.AzureMarkdownRewriterTool
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.AzureMarkdownRewriters;

    public class AzureFileInformationCollection
    {
        public AzureFileInformationCollection()
        {
            AzureMarkdownFileInfoMapping = new Dictionary<string, AzureFileInfo>();
            AzureResourceFileInfoMapping = new Dictionary<string, AzureFileInfo>();
            AzureIncludeMarkdownFileInfoMapping = new Dictionary<string, AzureFileInfo>();
            AzureIncludeResourceFileInfoMapping = new Dictionary<string, AzureFileInfo>();
            AzureVideoInfoMapping = new Dictionary<string, AzureVideoInfo>();
        }

        /// <summary>
        /// Azure markdown file info mapping. Not contain the markdown in includes folder
        /// key: file name
        /// value: azure file info
        /// </summary>
        public Dictionary<string, AzureFileInfo> AzureMarkdownFileInfoMapping { get; set; }

        /// <summary>
        /// Azure resource file info mapping. Not contain the resource in includes folder
        /// key: file name
        /// value: azure file info
        /// </summary>
        public Dictionary<string, AzureFileInfo> AzureResourceFileInfoMapping { get; set; }

        /// <summary>
        /// Azure markdown file info mapping in includes folder
        /// key: file name
        /// value: azure file info
        /// </summary>
        public Dictionary<string, AzureFileInfo> AzureIncludeMarkdownFileInfoMapping { get; set; }

        /// <summary>
        /// Azure markdown file info mapping in includes folder
        /// key: file name
        /// value: azure file info
        /// </summary>
        public Dictionary<string, AzureFileInfo> AzureIncludeResourceFileInfoMapping { get; set; }

        /// <summary>
        /// Azure video info mapping
        /// key: video id
        /// value: azure video info
        /// </summary>
        public Dictionary<string, AzureVideoInfo> AzureVideoInfoMapping { get; set; }
    }
}
