// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    public class AzureFileInfo
    {
        /// <summary>
        /// Indicate the azure file name. It's global unique in azure content
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Indicate currently absolute path of azure file
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Indicate whether the current relative path need to be changed to external link.
        /// If the current file is in docset, then false. Otherwise true.
        /// </summary>
        public bool NeedTransformToAzureExternalLink { get; set; }

        /// <summary>
        /// Indicate the uri prefix except the asset id
        /// </summary>
        public string UriPrefix { get; set; }
    }
}
