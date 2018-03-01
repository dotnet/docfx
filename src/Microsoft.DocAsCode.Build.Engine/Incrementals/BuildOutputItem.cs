// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System.Collections.Generic;

    public class BuildOutputItem
    {
        /// <summary>
        /// The source file, always from working folder, i.e. start with "~/"
        /// </summary>
        public string SourceFile { get; set; }
        /// <summary>
        /// The destination file without extension
        /// </summary>
        public string DestinationFile { get; set; }
        /// <summary>
        /// The output files, key is extension name (e.g. ".html"), the value is file path (can be random name).
        /// </summary>
        public Dictionary<string, string> Files { get; set; }
        /// <summary>
        /// The metadata in manifest for this item.
        /// </summary>
        public Dictionary<string, object> ManifestMetadata { get; set; }
    }
}
