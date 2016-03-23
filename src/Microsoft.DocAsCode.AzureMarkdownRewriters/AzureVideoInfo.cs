// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    public class AzureVideoInfo
    {
        /// <summary>
        /// Indicate the azure video id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Indicate the azure video link
        /// </summary>
        public string Link { get; set; }

        /// <summary>
        /// Indicate the video width
        /// </summary>
        public int Width { get; set; } = 640;

        /// <summary>
        /// Indicate the video height
        /// </summary>
        public int Height { get; set; } = 360;
    }
}
