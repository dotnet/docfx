// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;

    public class BuildOutputItem
    {
        /// <summary>
        /// The source file, always form working folder, i.e. start with "~/"
        /// </summary>
        public string SourceFile { get; set; }
        /// <summary>
        /// The output files, key is extension name (e.g. ".html"), the value is file path.
        /// </summary>
        public Dictionary<string, string> Files { get; set; }
        /// <summary>
        /// The metadata in manifest for this item.
        /// </summary>
        public Dictionary<string, object> ManifestMetadata { get; set; }
    }
}
