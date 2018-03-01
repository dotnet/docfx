// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;

    public class FileAttributeItem
    {
        /// <summary>
        /// The file path
        /// </summary>
        public string File { get; set; }
        /// <summary>
        /// Last modified time
        /// </summary>
        public DateTime LastModifiedTime { get; set; }
        /// <summary>
        /// MD5 string of the file content
        /// </summary>
        public string MD5 { get; set; }

        /// <summary>
        /// Whether the file is from source or dependency
        /// </summary>
        public bool IsFromSource { get; set; }
    }
}
